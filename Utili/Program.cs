﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBotsList.Api;
using DiscordBotsList.Api.Objects;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using static Utili.Data;
using static Utili.Json;
using static Utili.SendMessage;
using Timer = System.Timers.Timer;

namespace Utili
{
    internal class Program
    {
        public static string VersionNumber = "1.11.11";

        // ReSharper disable InconsistentNaming
        public static DiscordSocketClient _client;
        public static DiscordShardedClient _shards;
        public static CommandService _commands;
        public static YouTubeService _youtube;
        // ReSharper restore InconsistentNaming

        public static CancellationTokenSource ForceStop;
        public static Timer ReliabilityTimer;
        public static Timer LatencyTimer;
        public static int TotalShards;
        public static int ShardId = -1;
        public static bool Ready;
        public static bool FirstStart = true;
        public static int Restarts = -1;

        public static bool Debug = false;
        public static bool UseTest = false;

        private DateTime _lastStatsUpdate = DateTime.Now;

        #region System

        private static void Main()
        {
            if (!Debug)
            {
                Console.WriteLine("See Output.txt for console.");
                StreamWriter outputFile;

                if (!File.Exists("Output.txt")) outputFile = File.CreateText("Output.txt");
                else outputFile = File.AppendText("Output.txt");
                outputFile.AutoFlush = true;
                Console.SetOut(outputFile);
                Console.SetError(outputFile);
            }

            bool retry = true;

            while (true)
            {
                try
                {
                    if (retry)
                    {
                        Console.WriteLine($"[{DateTime.Now}] [Info] Starting MainAsync.");
                        retry = false;
                        ForceStop = new CancellationTokenSource();
                        Ready = false;
                        new Program().MainAsync().GetAwaiter().GetResult();
                    }
                }
                catch (Exception e)
                {
                    try
                    {
                        if (_client.ConnectionState != ConnectionState.Connected)
                        {
                            if (e.InnerException == null) Console.WriteLine($"[{DateTime.Now}] [Crash] {e.Message}\n\nRestarting...\n\n");
                            else Console.WriteLine($"[{DateTime.Now}] [Crash] {e.Message}\n{e.InnerException.Message}\n\nRestarting...\n\n");

                            try { ForceStop.Cancel(); } catch { }

                            try { ReliabilityTimer.Stop(); } catch { }
                            try { ReliabilityTimer.Dispose(); } catch { }

                            try { _client.StopAsync(); } catch { }

                            try { _client.Dispose(); } catch { }

                            try { Autopurge.StartRunthrough.Stop(); } catch { }
                            try { InactiveRole.StartRunthrough.Stop(); } catch { }

                            try { LatencyTimer.Stop(); } catch { }
                            try { LatencyTimer.Dispose(); } catch { }

                            Ready = false;

                            Thread.Sleep(30000);

                            retry = true;
                        }
                        else Console.WriteLine($"[{DateTime.Now}] [Exception] {e.Message}");
                    }
                    catch //Only if _client.ConnectionState errors
                    {
                        Console.WriteLine($"[{DateTime.Now}] [Crash] {e.Message}\n\nRestarting...\n");
                        retry = true;
                        Thread.Sleep(30000);
                    }
                }
            }
        }

        private async Task MainAsync()
        {
            Ready = false;

            if (!LoadConfig()) GenerateNewConfig();
            SetConnectionString();

            try
            {
                await _client.StopAsync();
                await _shards.StopAsync();
                _client.Dispose();
                _shards.Dispose();
            }
            catch { }

            ShardId = -1;

            if (UseTest)
            {
                ShardId = 0;
                TotalShards = 1;
            }

            TotalShards = 1;

            if (!UseTest)
            {
                TotalShards = await Sharding.GetTotalShards();
                Console.WriteLine($"[{DateTime.Now}] [Sharding] Waiting for a shard to become available (0-{TotalShards - 1})");
                ShardId = await Sharding.GetShardId();
                Console.WriteLine($"[{DateTime.Now}] [Sharding] Found available shard {ShardId}. Continuing with startup.");
            }

            if (!UseTest)
            {
                _ = Sharding.KeepConnection();
                _ = Sharding.FlushDisconnected();
            }

            _shards = new DiscordShardedClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 5,
                TotalShards = TotalShards,
                ConnectionTimeout = 30000,
                AlwaysDownloadUsers = true,
                ExclusiveBulkDelete = true,

                GatewayIntents = 
                    GatewayIntents.GuildEmojis |
                    GatewayIntents.GuildIntegrations |
                    GatewayIntents.GuildMembers |
                    GatewayIntents.GuildMessageReactions |
                    GatewayIntents.GuildMessages |
                    GatewayIntents.Guilds |
                    GatewayIntents.GuildVoiceStates |
                    GatewayIntents.GuildWebhooks
            });

            _client = _shards.GetShard(ShardId);

            _commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async,
                LogLevel = LogSeverity.Debug
            });

            _client.MessageReceived += Commence_MessageReceived;
            _client.MessageDeleted += Commence_MessageDelete;
            _client.MessageUpdated += Commence_MessageUpdated;
            _client.UserJoined += Commence_UserJoin;
            _client.UserLeft += Commence_UserLeft;
            _client.UserVoiceStateUpdated += Commence_UserVoiceStateUpdated;
            _client.ChannelCreated += Commence_ChannelCreated;
            _client.ReactionAdded += Commence_ReactionAdded;
            _client.ReactionRemoved += Commence_ReactionRemoved;
            _client.JoinedGuild += Commence_ClientJoin;
            _client.LeftGuild += Commence_ClientLeave;

            _commands.AddTypeReader(typeof(TimeSpan), new TimespanTypeReader());

            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);

            _client.Ready += Client_Ready;
            _client.Connected += Client_Connected;
            _client.Log += Client_Log;

            Console.WriteLine($"[{DateTime.Now}] [Info] Starting bot on version {VersionNumber}");

            string token;

            if (!UseTest) token = Config.Token;
            else token = Config.TestToken;

            QueryTimer = DateTime.Now;
            Queries = 0;
            CacheQueries = 0;

            await _shards.LoginAsync(TokenType.Bot, token);

            await _client.SetGameAsync("Starting up...");

            await _shards.StartAsync();

            LatencyTimer = new Timer(10000);
            LatencyTimer.Elapsed += UpdateLatency;
            LatencyTimer.Start();

            Autopurge autopurge = new Autopurge();
            InactiveRole inactiveRole = new InactiveRole();

            _ = autopurge.Run();
            _ = inactiveRole.Run();

            ForceStop = new CancellationTokenSource();

            ReliabilityTimer = new Timer(5000);
            ReliabilityTimer.Elapsed += CheckReliability;
            ReliabilityTimer.Start();

            try { await Task.Delay(-1, ForceStop.Token); } catch { }

            try { ForceStop.Cancel(); } catch { }

            try { ReliabilityTimer.Stop(); } catch { }
            try { ReliabilityTimer.Dispose(); } catch { }

            try { await _client.StopAsync(); } catch { }
            try { _client.Dispose(); } catch { }

            Autopurge.StartRunthrough.Stop();
            InactiveRole.StartRunthrough.Stop();

            try { LatencyTimer.Stop(); } catch { }
            try { LatencyTimer.Dispose(); } catch { }

            Ready = false;

            Console.WriteLine($"[{DateTime.Now}] [Info] MainAsync will terminate with an error in 5 seconds.");
            await Task.Delay(5000);
            throw new Exception("MainAsync was terminated.");
        }

        

        private async void UpdateLatency(object sender, ElapsedEventArgs e)
        {
            if (!Ready) return;

            try
            {
                TimeSpan uptime = DateTime.Now - QueryTimer;
                try { QueriesPerSecond = Math.Round(Queries / uptime.TotalSeconds, 2); } catch { QueriesPerSecond = 0; }
                try { CacheQueriesPerSecond = Math.Round(CacheQueries / uptime.TotalSeconds, 2); } catch { CacheQueriesPerSecond = 0; }
                try { ReactionsAlteredPerSecond = Math.Round(ReactionsAltered / uptime.TotalSeconds, 2); } catch { ReactionsAltered = 0; }

                if(ReactionsAlteredPerSecond > MaxReactionsAlteredPerSecond)
                {
                    MaxReactionsAlteredPerSecond = ReactionsAlteredPerSecond;
                }

                try
                {
                    DateTime now = DateTime.Now;
                    GetData("Ping Test", ignoreCache: true);
                    DbLatency = (int)Math.Round((DateTime.Now - now).TotalMilliseconds);
                }
                catch { }

                CacheItems = Cache.Count;

                string output = "";

                IEnumerable<IGrouping<string, Data>> grouped = CommonItemsRegistry.GroupBy(x => x.Type);
                IOrderedEnumerable<IGrouping<string, Data>> sorted = grouped.OrderByDescending(x => x.Count());

                foreach (IGrouping<string, Data> value in sorted.Take(8))
                {
                    output += $"{value.Count()}: {value.Key}\n";
                }

                CommonItemsOutput = output;

                output = "";

                foreach (Data value in CommonItemsSaved.Take(8))
                {
                    output += $"{value}\n";
                }

                CommonItemsSavedOutput = output;

                output = "";

                foreach (Data value in CommonItemsGot.Take(8))
                {
                    output += $"{value}\n";
                }

                CommonItemsGotOutput = output;

                CommonItemsRegistry.Clear();
                CommonItemsGot.Clear();
                CommonItemsSaved.Clear();

                QueryTimer = DateTime.Now;
                Queries = 0;
                CacheQueries = 0;
                ReactionsAltered = 0;

                try
                {
                    DateTime now = DateTime.Now;
                    RestUserMessage sent = await _shards.GetGuild(682882628168450079).GetTextChannel(713125991563919492).SendMessageAsync("Testing send latency...");
                    SendLatency = (int)Math.Round((DateTime.Now - now).TotalMilliseconds);

                    now = DateTime.Now;
                    await sent.ModifyAsync(x => x.Content = "Testing edit latency...");
                    EditLatency = (int)Math.Round((DateTime.Now - now).TotalMilliseconds);

                    await sent.DeleteAsync();
                }
                catch { SendLatency = 0; EditLatency = 0; }
            }
            catch { }
        }

        private async void CheckReliability(object sender, ElapsedEventArgs e)
        {
            if (_client.ConnectionState != ConnectionState.Connected)
            {
                for (int i = 0; i < 30; i++)
                {
                    try { await Task.Delay(1000, ForceStop.Token); } catch { }
                    if ((_client.ConnectionState == ConnectionState.Connected) || ForceStop.IsCancellationRequested || !Ready) return;
                }

                Console.WriteLine($"[{DateTime.Now}] [Info] Script terminated due to prolonged disconnect [{_client.ConnectionState} @ {_client.Latency}ms]");
                Ready = false;
                ForceStop.Cancel();
            }
        }

        private async Task Client_Log(LogMessage message)
        {
            if (!message.Message.Contains("PRESENCE_UPDATE") && !message.Message.Contains("Unknown dispatch"))
            {
                Console.WriteLine($"[{DateTime.Now}] [{message.Source}] {message.Message}");
            }
        }

        private async Task Client_Ready()
        {
            Console.WriteLine($"[{DateTime.Now}] [Info] Logged in as bot user {_client.CurrentUser} ({_client.CurrentUser.Id})");
            Restarts += 1;

            if (FirstStart)
            {
                string guildArray = "";
                foreach (SocketGuild guild in _client.Guilds) guildArray += $"'{guild.Id}',";
                guildArray = guildArray.Remove(guildArray.Length - 1);

                Console.WriteLine($"[{DateTime.Now}] [Info] Loading cache for {_client.Guilds.Count} guilds...");

                Cache = GetDataWhere($"GuildID IN ({guildArray}) AND DataType NOT LIKE '%RolePersist-Role-%'");

                Console.WriteLine($"[{DateTime.Now}] [Info] {Cache.Count} items loaded.");

                FirstStart = false;
            }
            else Console.WriteLine($"[{DateTime.Now}] [Info] Skipped cache loading as this is not the first startup.");

            _youtube = new YouTubeService(new BaseClientService.Initializer
            {
                ApplicationName = Config.Youtube.ApplicationName,
                ApiKey = Config.Youtube.Key
            });

            _ = UpdateStats();

            AntiProfane antiProfane = new AntiProfane();
            _ = antiProfane.AntiProfane_Ready();

            Ready = true;

            _ = Task.Run(async () =>
            {
                await Task.Delay(10000);

                string machine = "Undefined Machine";
                try { machine = Environment.MachineName; } catch { }
                if (machine == null) machine = "Undefined Machine";

                bool success = false;
                while (!success)
                {
                    try
                    {
                        SocketGuild guild = _shards.GetGuild(682882628168450079);
                        SocketTextChannel channel = guild.GetTextChannel(731790673728241665);

                        if (Restarts == 0) await channel.SendMessageAsync(embed: GetEmbed("Yes", "Checking in", $"Shard {ShardId} is ready, running v{VersionNumber} on {machine}. This is the fist startup."));
                        else if (Restarts == 1) await channel.SendMessageAsync(embed: GetEmbed("Yes", "Checking in", $"Shard {ShardId} is ready, running v{VersionNumber} on {machine}. Since first startup {Restarts} restart has occurred."));
                        else await channel.SendMessageAsync(embed: GetEmbed("Yes", "Checking in", $"Shard {ShardId} is ready, running v{VersionNumber} on {machine}. Since first startup {Restarts} restarts have occurred."));

                        success = true;
                        Console.WriteLine($"[{DateTime.Now}] [Info] Sent check-in to log channel.");
                    }
                    catch { await Task.Delay(5000); }
                }
            });
        }

        private async Task Client_Connected()
        {
            _ = Task.Run(async () =>
            {
                foreach(SocketGuild guild in _client.Guilds)
                {
                    await guild.DownloadUsersAsync();
                }

                await _client.SetGameAsync(".help", null, ActivityType.Watching);
            });
        }

        #endregion System

        #region Receive

        private async Task Commence_MessageReceived(SocketMessage messageParam)
        {
            Task.Run(() =>
            {
                Client_MessageReceived(messageParam);
            });
        }

        private async Task Client_MessageReceived(SocketMessage messageParam)
        {
            #region Delete System Messages

            if (messageParam.Author.Id == _client.CurrentUser.Id & messageParam.GetType() == typeof(SocketSystemMessage))
            {
                await messageParam.DeleteAsync();
                return;
            }

            #endregion Delete System Messages

            #region System

            SocketUserMessage message = messageParam as SocketUserMessage;
            SocketCommandContext context = new SocketCommandContext(_client, message);

            #endregion System

            #region Reject DMs

            if (message.Channel.GetType() == typeof(SocketDMChannel) & !context.User.IsBot)
            {
                return;
            }

            #endregion Reject DMs

            #region Command Handler
            
            if (!(context.Message == null || context.Message.ToString() == "" || context.User.Id == _client.CurrentUser.Id || context.User.IsBot))
            {
                if (!DataExists(context.Guild.Id.ToString(), "Commands-Disabled", context.Channel.Id.ToString()))
                {
                    int argPos = 0;

                    string prefix = ".";
                    try { prefix = GetFirstData(context.Guild.Id.ToString(), "Prefix").Value; } catch { }

                    if (UseTest) prefix = "-";

                    if (message.HasStringPrefix(prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                    {
                        try
                        {
                            IResult result = await _commands.ExecuteAsync(context, argPos, null);

                            if (!result.IsSuccess)
                            {
                                if (result.ErrorReason != "Unknown command.")
                                {
                                    await context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));

                                    #region Command Logging

                                    StreamWriter sw;
                                    if (!File.Exists("Command Log.txt")) sw = File.CreateText("Command Log.txt");
                                    else sw = File.AppendText("Command Log.txt");

                                    sw.WriteLine($"[{DateTime.Now}] [Command] [{context.Guild.Name} | {context.Guild.Id}] [{context.Channel.Name} | {context.Channel.Id}] [{context.User} | {context.User.Id}] {context.Message.Content}");

                                    sw.Close();

                                    #endregion Command Logging

                                    #region Command Error Logging

                                    if (!File.Exists("Command Log.txt")) sw = File.CreateText("Command Log.txt");
                                    else sw = File.AppendText("Command Log.txt");

                                    sw.WriteLine($"[{DateTime.Now}] [Command] [Error] {result.ErrorReason}");

                                    sw.Close();

                                    #endregion Command Error Logging
                                }
                            }
                            else
                            {
                                #region Command Logging

                                StreamWriter sw;
                                if (!File.Exists("Command Log.txt")) sw = File.CreateText("Command Log.txt");
                                else sw = File.AppendText("Command Log.txt");

                                sw.WriteLine($"[{DateTime.Now}] [Command] [{context.Guild.Name} | {context.Guild.Id}] [{context.Channel.Name} | {context.Channel.Id}] [{context.User} | {context.User.Id}] {context.Message.Content}");

                                sw.Close();

                                #endregion Command Logging
                            }
                        }
                        catch
                        {
                            await context.Channel.SendMessageAsync(embed: GetEmbed("No", "An unexpected error occurred", "Please report this"));
                        }
                    }
                }
            }

            #endregion Command Handler

            #region Start other scripts

            MessageLogs messageLogs = new MessageLogs();
            _ = messageLogs.MessageLogs_MessageReceived(messageParam);

            SpamFilter spamFilter = new SpamFilter();
            _ = spamFilter.SpamFilter_MessageReceived(messageParam);

            Filter filter = new Filter();
            _ = filter.Filter_MessageReceived(messageParam);

            AntiProfane antiProfane = new AntiProfane();
            _ = antiProfane.AntiProfane_MessageReceived(messageParam);

            Votes votes = new Votes();
            _ = votes.Votes_MessageReceived(messageParam);

            NoticeMessage noticeMessage = new NoticeMessage();
            _ = noticeMessage.NoticeMessage_MessageReceived(messageParam);

            Mirroring mirroring = new Mirroring();
            _ = mirroring.Mirroring_MessageReceived(messageParam);

            InactiveRole inactiveRole = new InactiveRole();
            _ = inactiveRole.InactiveRole_MessageReceived(messageParam);

            if (_lastStatsUpdate < DateTime.Now.AddMinutes(-10)) _ = UpdateStats();

            #endregion Start other scripts
        }

        #endregion Receive

        #region Update Stats

        private async Task UpdateStats()
        {
            if (UseTest) return;

            _lastStatsUpdate = DateTime.Now;

            #region top.gg

            AuthDiscordBotListApi api = new AuthDiscordBotListApi(655155797260501039, Config.DiscordBotListKey);
            IDblSelfBot me = await api.GetMeAsync();
            try { await me.UpdateStatsAsync(_shards.Guilds.Count); } catch { }

            #endregion top.gg

            #region Bots For Discord

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Config.BotsForDiscordKey);

            FormUrlEncodedContent content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("server_count", _shards.Guilds.Count.ToString()),
            });

            try { await httpClient.PostAsync("https://botsfordiscord.com/api/bot/655155797260501039", content); } catch { }

            #endregion Bots For Discord

            #region Bots On Discord

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Config.BotsOnDiscordKey);

            content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("guildCount", _shards.Guilds.Count.ToString()),
            });

            try { await httpClient.PostAsync("https://bots.ondiscord.xyz/bot-api/bots/655155797260501039/guilds", content); } catch { }

            #endregion Bots On Discord

            #region Discord Boats

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Config.DiscordBoatsKey);

            content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("server_count", _shards.Guilds.Count.ToString()),
            });

            try { await httpClient.PostAsync("https://discord.boats/api/bot/655155797260501039", content); } catch { }

            #endregion Discord Boats
        }

        #endregion Update Stats

        #region User Join

        private async Task Commence_UserJoin(SocketGuildUser user)
        {
            Task.Run(() =>
            {
                Client_UserJoin(user);
            });
        }

        private async Task Client_UserJoin(SocketGuildUser user)
        {
            RolePersist rolePersist = new RolePersist();
            _ = rolePersist.UserJoin(user);

            JoinMessage joinMessage = new JoinMessage();
            _ = joinMessage.JoinMessage_UserJoined(user);

            DeleteData(user.Guild.Id.ToString(), $"InactiveRole-Timer-{user.Id}", ignoreCache: true, table: "Utili_InactiveTimers");
            SaveData(user.Guild.Id.ToString(), $"InactiveRole-Timer-{user.Id}", ToSqlTime(DateTime.Now), ignoreCache: true, table: "Utili_InactiveTimers");

            SocketRole role = user.Guild.GetRole(ulong.Parse(GetFirstData(user.Guild.Id.ToString(), "JoinRole").Value));
            await user.AddRoleAsync(role);
        }

        #endregion User Join

        #region Client Join

        private async Task Commence_ClientJoin(SocketGuild guild)
        {
            Task.Run(() =>
            {
                Client_ClientJoin(guild);
            });
        }

        private async Task Client_ClientJoin(SocketGuild guild)
        {
            DeleteData(guild.Id.ToString(), ignoreCache: true, table: "Utili_InactiveTimers");
            DateTime startTime = DateTime.Now;
            foreach (SocketGuildUser user in guild.Users)
            {
                SaveData(user.Guild.Id.ToString(), $"InactiveRole-Timer-{user.Id}", ToSqlTime(startTime), ignoreCache: true, table: "Utili_InactiveTimers");
                await Task.Delay(100);
            }
        }

        #endregion _client Join

        #region User Leave

        private async Task Commence_UserLeft(SocketGuildUser user)
        {
            Task.Run(() =>
            {
                Client_UserLeft(user);
            });
        }

        private async Task Client_UserLeft(SocketGuildUser user)
        {
            DeleteData(user.Guild.Id.ToString(), $"InactiveRole-Timer-{user.Id}", ignoreCache: true, table: "Utili_InactiveTimers");

            RolePersist rolePersist = new RolePersist();
            _ = rolePersist.UserLeft(user);
        }

        #endregion User Leave

        #region Client Leave

        private async Task Commence_ClientLeave(SocketGuild guild)
        {
            Task.Run(() =>
            {
                Client_ClientLeave(guild);
            });
        }

        private async Task Client_ClientLeave(SocketGuild guild)
        {
            DeleteData(guild.Id.ToString(), ignoreCache: true, table: "Utili_InactiveTimers");
            RunNonQuery($"DELETE FROM Utili_MessageLogs WHERE GuildID = '{guild.Id}'");
            DeleteData(guild.Id.ToString());
        }

        #endregion _client Leave

        #region Voice Updated

        private async Task Commence_UserVoiceStateUpdated(SocketUser userParam, SocketVoiceState before, SocketVoiceState after)
        {
            Task.Run(() =>
            {
                Client_UserVoiceStateUpdated(userParam, before, after);
            });
        }

        private async Task Client_UserVoiceStateUpdated(SocketUser userParam, SocketVoiceState before, SocketVoiceState after)
        {
            SocketGuildUser user = userParam as SocketGuildUser;
            VcLink vcLink = new VcLink();
            _ = vcLink.Client_UserVoiceStateUpdated(user, before, after);
            VcRoles vcRoles = new VcRoles();
            _ = vcRoles.Client_UserVoiceStateUpdated(user, before, after);

            SocketGuild guild = user.Guild;

            if (after.VoiceChannel != null && before.VoiceChannel == null)
            {
                Data inactiveRole = GetFirstData(guild.Id.ToString(), "InactiveRole-Role");
                if (inactiveRole != null)
                {
                    SocketRole role = guild.GetRole(ulong.Parse(inactiveRole.Value));

                    if (user.Roles.Select(x => x.Id).Contains(role.Id))
                    {
                        await user.RemoveRoleAsync(role);
                    }

                    int rowsAffected = RunNonQuery("UPDATE Utili_InactiveTimers SET DataValue = @Value WHERE GuildID = @GuildID AND DataType = @Type", new[] { ("GuildID", guild.Id.ToString()), ("Type", $"InactiveRole-Timer-{user.Id}"), ("Value", ToSqlTime(DateTime.Now)) });
                    if (rowsAffected == 0)
                    {
                        SaveData(guild.Id.ToString(), $"InactiveRole-Timer-{user.Id}", ToSqlTime(DateTime.Now), ignoreCache: true, table: "Utili_InactiveTimers");
                    }
                }
            }
        }

        #endregion Voice Updated

        #region Channel Created

        private async Task Commence_ChannelCreated(SocketChannel channel)
        {
            Task.Run(() =>
            {
                Client_ChannelCreated(channel);
            });
        }

        private async Task Client_ChannelCreated(SocketChannel channel)
        {
            MessageLogs messageLogs = new MessageLogs();
            Task.Run(() =>
            {
                messageLogs.MessageLogs_ChannelCreated(channel);
            });
        }

        #endregion Channel Created

        #region Messages

        public async Task Commence_MessageDelete(Cacheable<IMessage, ulong> partialMessage, ISocketMessageChannel channel)
        {
            MessageLogs messageLogs = new MessageLogs();
            Task.Run(() =>
            {
                messageLogs.MessageLogs_MessageDeleted(partialMessage, channel);
            });
        }

        public async Task Commence_MessageUpdated(Cacheable<IMessage, ulong> partialMessage, SocketMessage newMessage, ISocketMessageChannel channel)
        {
            MessageLogs messageLogs = new MessageLogs();
            Task.Run(() =>
            {
                messageLogs.MessageLogs_MessageEdited(partialMessage, newMessage, channel);
            });
        }

        public async Task Commence_ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            ReactionsAltered += 1;
            Votes votes = new Votes();
            Task.Run(() =>
            {
                votes.Votes_ReactionAdded(message, channel, reaction);
            });
        }

        public async Task Commence_ReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            ReactionsAltered += 1;
        }

        #endregion Messages
    }
}