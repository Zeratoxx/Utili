using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBotsList.Api;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static Utili.Data;
using static Utili.Json;
using static Utili.SendMessage;

namespace Utili
{
    internal class Program
    {
        public static string VersionNumber = "1.11.7";

        public static DiscordSocketClient Client;
        public static DiscordShardedClient Shards;
        private CommandService Commands;
        public static YouTubeService Youtube;
        public static CancellationTokenSource ForceStop;
        public static System.Timers.Timer ReliabilityTimer;
        public static System.Timers.Timer LatencyTimer;
        public static int TotalShards = 0;
        public static int ShardID = 0;
        public static bool Ready = false;
        public static bool FirstStart = true;
        public static int Restarts = -1;

        public static bool Debug = false;
        public static bool UseTest = false;

        private DateTime LastStatsUpdate = DateTime.Now;

        #region System

        private static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) & UseTest == false)
            {
                Console.WriteLine($"WARNING:\nRunning Utili on a non-linux machine!\n\nUse the test bot if you've changed code.\nPress Y to continue...");
                if (Console.ReadKey().Key != ConsoleKey.Y) Environment.Exit(0);
                Console.Clear();
            }

            if (!Debug)
            {
                Console.WriteLine("See Output.txt for console.");
                StreamWriter outputFile = null;

                if (!File.Exists("Output.txt")) outputFile = File.CreateText("Output.txt");
                else outputFile = File.AppendText("Output.txt");
                outputFile.AutoFlush = true;
                Console.SetOut(outputFile);
                Console.SetError(outputFile);
            }

            bool Retry = true;

            while (true)
            {
                try
                {
                    if (Retry)
                    {
                        Console.WriteLine($"[{DateTime.Now}] [Info] Starting MainAsync.");
                        Retry = false;
                        ForceStop = new CancellationTokenSource();
                        Ready = false;
                        new Program().MainAsync().GetAwaiter().GetResult();
                    }
                }
                catch (Exception e)
                {
                    try
                    {
                        if (Client.ConnectionState != ConnectionState.Connected)
                        {
                            if (e.InnerException == null) Console.WriteLine($"[{DateTime.Now}] [Crash] {e.Message}\n\nRestarting...\n\n");
                            else Console.WriteLine($"[{DateTime.Now}] [Crash] {e.Message}\n{e.InnerException.Message}\n\nRestarting...\n\n");

                            try { ForceStop.Cancel(); } catch { }

                            try { ReliabilityTimer.Stop(); } catch { }
                            try { ReliabilityTimer.Dispose(); } catch { }

                            try { Client.StopAsync(); } catch { };
                            try { Client.Dispose(); } catch { };

                            try { Autopurge.StartRunthrough.Stop(); } catch { }
                            try { InactiveRole.StartRunthrough.Stop(); } catch { }

                            try { LatencyTimer.Stop(); } catch { }
                            try { LatencyTimer.Dispose(); } catch { }

                            Ready = false;

                            Thread.Sleep(5000);

                            Retry = true;
                        }
                        else Console.WriteLine($"[{DateTime.Now}] [Exception] {e.Message}");
                    }
                    catch //Only if Client.ConnectionState errors
                    {
                        Console.WriteLine($"[{DateTime.Now}] [Crash] {e.Message}\n\nRestarting...\n");
                        Retry = true;
                        Thread.Sleep(5000);
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
                await Client.StopAsync();
                await Shards.StopAsync();
                Client.Dispose();
                Shards.Dispose();
            }
            catch { }

            ShardID = 0;
            TotalShards = 1;

            if (!UseTest)
            {
                TotalShards = await Sharding.GetTotalShards();
                Console.WriteLine($"[{DateTime.Now}] [Sharding] Waiting for a shard to become available (0-{TotalShards - 1})");
                ShardID = await Sharding.GetShardID();
                Console.WriteLine($"[{DateTime.Now}] [Sharding] Found available shard {ShardID}. Continuing with startup.");
            }

            if (!UseTest)
            {
                _ = Sharding.KeepConnection();
                _ = Sharding.FlushDisconnected();
            }

            Shards = new DiscordShardedClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 100,
                TotalShards = TotalShards,
                ConnectionTimeout = 15000
            });

            Client = Shards.GetShard(ShardID);

            Commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async,
                LogLevel = LogSeverity.Debug,
            });

            Client.MessageReceived += Commence_MessageReceived;
            Client.MessageDeleted += Commence_MessageDelete;
            Client.MessageUpdated += Commence_MessageUpdated;
            Client.UserJoined += Commence_UserJoin;
            Client.UserLeft += Commence_UserLeft;
            Client.UserVoiceStateUpdated += Commence_UserVoiceStateUpdated;
            Client.ChannelCreated += Commence_ChannelCreated;
            Client.ReactionAdded += Commence_ReactionAdded;
            Client.JoinedGuild += Commence_ClientJoin;
            Client.LeftGuild += Commence_ClientLeave;

            Commands.AddTypeReader(typeof(TimeSpan), new TimespanTypeReader());

            await Commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);

            Client.Ready += Client_Ready;
            Client.Log += Client_Log;

            Console.WriteLine($"[{DateTime.Now}] [Info] Starting bot on version {VersionNumber}");

            string Token;

            if (!UseTest) Token = Config.Token;
            else Token = Config.TestToken;

            QueryTimer = DateTime.Now;
            Queries = 0;
            CacheQueries = 0;

            await Shards.LoginAsync(TokenType.Bot, Token);
            await Shards.StartAsync();

            LatencyTimer = new System.Timers.Timer(10000);
            LatencyTimer.Elapsed += UpdateLatency;
            LatencyTimer.Start();

            Autopurge Autopurge = new Autopurge();
            InactiveRole InactiveRole = new InactiveRole();

            _ = Autopurge.Run();
            _ = InactiveRole.Run();

            ForceStop = new CancellationTokenSource();

            ReliabilityTimer = new System.Timers.Timer(5000);
            ReliabilityTimer.Elapsed += CheckReliability;
            ReliabilityTimer.Start();

            try { await Task.Delay(-1, ForceStop.Token); } catch { }

            try { ForceStop.Cancel(); } catch { }

            try { ReliabilityTimer.Stop(); } catch { }
            try { ReliabilityTimer.Dispose(); } catch { }

            try { await Client.StopAsync(); } catch { };
            try { Client.Dispose(); } catch { };

            Autopurge.StartRunthrough.Stop();
            InactiveRole.StartRunthrough.Stop();

            try { LatencyTimer.Stop(); } catch { }
            try { LatencyTimer.Dispose(); } catch { }

            Ready = false;

            Console.WriteLine($"[{DateTime.Now}] [Info] MainAsync will terminate with an error in 5 seconds.");
            await Task.Delay(5000);
            throw new Exception("MainAsync was terminated.");
        }

        private async void UpdateLatency(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!Ready) return;

            try
            {
                TimeSpan Uptime = DateTime.Now - QueryTimer;
                try { QueriesPerSecond = Math.Round(Queries / Uptime.TotalSeconds, 2); } catch { QueriesPerSecond = 0; }
                try { CacheQueriesPerSecond = Math.Round(CacheQueries / Uptime.TotalSeconds, 2); } catch { CacheQueriesPerSecond = 0; }

                try
                {
                    DateTime Now = DateTime.Now;
                    GetFirstData("Ping Test", IgnoreCache: true);
                    DBLatency = (int)Math.Round((DateTime.Now - Now).TotalMilliseconds);
                }
                catch { };

                CacheItems = Cache.Count;

                QueryTimer = DateTime.Now;
                Queries = 0;
                CacheQueries = 0;

                try
                {
                    DateTime Now = DateTime.Now;
                    var Sent = await Shards.GetGuild(682882628168450079).GetTextChannel(713125991563919492).SendMessageAsync("Testing send latency...");
                    SendLatency = (int)Math.Round((DateTime.Now - Now).TotalMilliseconds);

                    Now = DateTime.Now;
                    await Sent.ModifyAsync(x => x.Content = "Testing edit latency...");
                    EditLatency = (int)Math.Round((DateTime.Now - Now).TotalMilliseconds);

                    await Sent.DeleteAsync();
                }
                catch { SendLatency = 0; EditLatency = 0; };

                
            }
            catch { };
        }

        private async void CheckReliability(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Client.ConnectionState != ConnectionState.Connected)
            {
                for(int i = 0; i < 30; i++)
                {
                    try { await Task.Delay(1000, ForceStop.Token); } catch { }
                    if (Client.ConnectionState == ConnectionState.Connected || ForceStop.IsCancellationRequested || !Ready) return;
                }
                
                Console.WriteLine($"[{DateTime.Now}] [Info] Script terminated due to prolonged disconnect [{Client.ConnectionState} @ {Client.Latency}ms]");
                Ready = false;
                ForceStop.Cancel();
            }
        }

        private async Task Client_Log(LogMessage Message)
        {
            if (Message.Source.ToString() != "Rest" & !Message.Message.Contains("PRESENCE_UPDATE"))
            {
                Console.WriteLine($"[{DateTime.Now}] [{Message.Source}] {Message.Message}");
            }
        }

        private async Task Client_Ready()
        {
            Console.WriteLine($"[{DateTime.Now}] [Info] Logged in as bot user {Client.CurrentUser} ({Client.CurrentUser.Id})");
            Restarts += 1;

            if (FirstStart)
            {
                string GuildArray = "";
                foreach (var Guild in Client.Guilds) GuildArray += $"'{Guild.Id}',";
                GuildArray = GuildArray.Remove(GuildArray.Length - 1);

                Console.WriteLine($"[{DateTime.Now}] [Info] Loading cache for {Client.Guilds.Count} guilds...");

                Cache = GetDataWhere($"GuildID IN ({GuildArray}) AND DataType NOT LIKE '%RolePersist-Role-%'");

                Console.WriteLine($"[{DateTime.Now}] [Info] {Cache.Count} items loaded.");

                FirstStart = false;
            }
            else Console.WriteLine($"[{DateTime.Now}] [Info] Skipped cache loading as this is not the first startup.");

            await Client.SetGameAsync(".help", null, ActivityType.Watching);

            Youtube = new YouTubeService(new BaseClientService.Initializer()
            {
                ApplicationName = Config.Youtube.ApplicationName,
                ApiKey = Config.Youtube.Key
            });

            UpdateStats();

            AntiProfane AntiProfane = new AntiProfane();
            Task.Run(() => AntiProfane.AntiProfane_Ready());

            Ready = true;

            _ = Task.Run(async () =>
            {
                await Task.Delay(10000);

                string Machine = "Undefined Machine";
                try { Machine = Environment.MachineName; } catch { }
                if (Machine == null) Machine = "Undefined Machine";

                bool Success = false;
                while (!Success)
                {
                    try
                    {
                        SocketGuild Guild = Shards.GetGuild(682882628168450079);
                        SocketTextChannel Channel = Guild.GetTextChannel(731790673728241665);

                        if (Restarts == 0) await Channel.SendMessageAsync(embed: GetEmbed("Yes", "Checking in", $"Shard {ShardID} is ready, running v{VersionNumber} on {Machine}. This is the fist startup."));
                        else if (Restarts == 1) await Channel.SendMessageAsync(embed: GetEmbed("Yes", "Checking in", $"Shard {ShardID} is ready, running v{VersionNumber} on {Machine}. Since first startup {Restarts} restart has occurred."));
                        else await Channel.SendMessageAsync(embed: GetEmbed("Yes", "Checking in", $"Shard {ShardID} is ready, running v{VersionNumber} on {Machine}. Since first startup {Restarts} restarts have occurred."));

                        Success = true;
                        Console.WriteLine($"[{DateTime.Now}] [Info] Sent check-in to log channel.");
                    }
                    catch { await Task.Delay(5000); }
                }
            });
        }

        #endregion System

        #region Receive

        private async Task Commence_MessageReceived(SocketMessage MessageParam)
        {
            Task.Run(() => Client_MessageReceived(MessageParam));
        }

        private async Task Client_MessageReceived(SocketMessage MessageParam)
        {
            #region Delete System Messages

            if (MessageParam.Author.Id == Client.CurrentUser.Id & MessageParam.GetType() == typeof(SocketSystemMessage))
            {
                await MessageParam.DeleteAsync();
                return;
            }

            #endregion Delete System Messages

            #region System

            var Message = MessageParam as SocketUserMessage;
            var Context = new SocketCommandContext(Client, Message);

            #endregion System

            var GuildUser = Context.User as SocketGuildUser;
            UserStatus Status = GuildUser.Status;

            #region Reject DMs

            if (Message.Channel.GetType() == typeof(SocketDMChannel) & !Context.User.IsBot)
            {
                return;
            }

            #endregion Reject DMs

            #region Command Handler

            if (!(Context.Message == null || Context.Message.ToString() == "" || Context.User.Id == Client.CurrentUser.Id || Context.User.IsBot))
            {
                if (!DataExists(Context.Guild.Id.ToString(), "Commands-Disabled", Context.Channel.Id.ToString()))
                {
                    int ArgPos = 0;

                    string Prefix = ".";
                    try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

                    if (UseTest) Prefix = "-";

                    if (Message.HasStringPrefix(Prefix, ref ArgPos) || Message.HasMentionPrefix(Client.CurrentUser, ref ArgPos))
                    {
                        try
                        {
                            var Result = await Commands.ExecuteAsync(Context, ArgPos, null);

                            if (!Result.IsSuccess)
                            {
                                if (Result.ErrorReason != "Unknown command.")
                                {
                                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {Prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));

                                    #region Command Logging

                                    StreamWriter sw;
                                    if (!File.Exists("Command Log.txt")) sw = File.CreateText("Command Log.txt");
                                    else sw = File.AppendText("Command Log.txt");

                                    sw.WriteLine($"[{DateTime.Now}] [Command] [{Context.Guild.Name} | {Context.Guild.Id}] [{Context.Channel.Name} | {Context.Channel.Id}] [{Context.User} | {Context.User.Id}] {Context.Message.Content}");

                                    sw.Close();

                                    #endregion Command Logging

                                    #region Command Error Logging

                                    if (!File.Exists("Command Log.txt")) sw = File.CreateText("Command Log.txt");
                                    else sw = File.AppendText("Command Log.txt");

                                    sw.WriteLine($"[{DateTime.Now}] [Command] [Error] {Result.ErrorReason}");

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

                                sw.WriteLine($"[{DateTime.Now}] [Command] [{Context.Guild.Name} | {Context.Guild.Id}] [{Context.Channel.Name} | {Context.Channel.Id}] [{Context.User} | {Context.User.Id}] {Context.Message.Content}");

                                sw.Close();

                                #endregion Command Logging
                            }
                        }
                        catch
                        {
                            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "An unexpected error occured", $"Please report this"));
                        }
                    }
                }
            }

            #endregion Command Handler

            #region Start other scripts

            MessageLogs MessageLogs = new MessageLogs();
            Task.Run(() => MessageLogs.MessageLogs_MessageReceived(MessageParam));

            SpamFilter SpamFilter = new SpamFilter();
            Task.Run(() => SpamFilter.SpamFilter_MessageReceived(MessageParam));

            Filter Filter = new Filter();
            Task.Run(() => Filter.Filter_MessageReceived(MessageParam));

            AntiProfane AntiProfane = new AntiProfane();
            Task.Run(() => AntiProfane.AntiProfane_MessageReceived(MessageParam));

            Votes Votes = new Votes();
            Task.Run(() => Votes.Votes_MessageReceived(MessageParam));

            NoticeMessage NoticeMessage = new NoticeMessage();
            Task.Run(() => NoticeMessage.NoticeMessage_MessageReceived(MessageParam));

            Mirroring Mirroring = new Mirroring();
            Task.Run(() => Mirroring.Mirroring_MessageReceived(MessageParam));

            InactiveRole InactiveRole = new InactiveRole();
            Task.Run(() => InactiveRole.InactiveRole_MessageReceived(MessageParam));

            if (LastStatsUpdate < DateTime.Now.AddMinutes(-10)) Task.Run(() => UpdateStats());

            #endregion Start other scripts
        }

        #endregion Receive

        #region Update Stats

        private async Task UpdateStats()
        {
            if (UseTest) return;

            LastStatsUpdate = DateTime.Now;

            #region top.gg

            AuthDiscordBotListApi API = new AuthDiscordBotListApi(655155797260501039, Config.DiscordBotListKey);
            var Me = await API.GetMeAsync();
            try { await Me.UpdateStatsAsync(Shards.Guilds.Count); } catch { }

            #endregion top.gg

            #region Bots For Discord

            HttpClient HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Config.BotsForDiscordKey);

            var Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("server_count", Shards.Guilds.Count.ToString()),
            });

            try { await HttpClient.PostAsync("https://botsfordiscord.com/api/bot/655155797260501039", Content); } catch { };

            #endregion Bots For Discord

            #region Bots On Discord

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Config.BotsOnDiscordKey);

            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("guildCount", Shards.Guilds.Count.ToString()),
            });

            try { await HttpClient.PostAsync("https://bots.ondiscord.xyz/bot-api/bots/655155797260501039/guilds", Content); } catch { };

            #endregion Bots On Discord

            #region Discord Boats

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Config.DiscordBoatsKey);

            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("server_count", Shards.Guilds.Count.ToString()),
            });

            try { await HttpClient.PostAsync("https://discord.boats/api/bot/655155797260501039", Content); } catch { };

            #endregion Discord Boats
        }

        #endregion Update Stats

        #region User Join

        private async Task Commence_UserJoin(SocketGuildUser User)
        {
            Task.Run(() => Client_UserJoin(User));
        }

        private async Task Client_UserJoin(SocketGuildUser User)
        {
            RolePersist RolePersist = new RolePersist();
            Task.Run(() => RolePersist.UserJoin(User));

            JoinMessage JoinMessage = new JoinMessage();
            Task.Run(() => JoinMessage.JoinMessage_UserJoined(User));

            DeleteData(User.Guild.Id.ToString(), $"InactiveRole-Timer-{User.Id}", IgnoreCache: true, Table: "Utili_InactiveTimers");
            SaveData(User.Guild.Id.ToString(), $"InactiveRole-Timer-{User.Id}", ToSQLTime(DateTime.Now), IgnoreCache: true, Table: "Utili_InactiveTimers");

            var Role = User.Guild.GetRole(ulong.Parse(GetFirstData(User.Guild.Id.ToString(), "JoinRole").Value));
            await User.AddRoleAsync(Role);
        }

        #endregion User Join

        #region Client Join

        private async Task Commence_ClientJoin(SocketGuild Guild)
        {
            Task.Run(() => Client_ClientJoin(Guild));
        }

        private async Task Client_ClientJoin(SocketGuild Guild)
        {
            DeleteData(Guild.Id.ToString(), IgnoreCache: true, Table: "Utili_InactiveTimers");
            DateTime StartTime = DateTime.Now;
            foreach (var User in Guild.Users)
            {
                SaveData(User.Guild.Id.ToString(), $"InactiveRole-Timer-{User.Id}", ToSQLTime(StartTime), IgnoreCache: true, Table: "Utili_InactiveTimers");
                await Task.Delay(100);
            }
        }

        #endregion Client Join

        #region User Leave

        private async Task Commence_UserLeft(SocketGuildUser User)
        {
            Task.Run(() => Client_UserLeft(User));
        }

        private async Task Client_UserLeft(SocketGuildUser User)
        {
            DeleteData(User.Guild.Id.ToString(), $"InactiveRole-Timer-{User.Id}", IgnoreCache: true, Table: "Utili_InactiveTimers");

            RolePersist RolePersist = new RolePersist();
            Task.Run(() => RolePersist.UserLeft(User));
        }

        #endregion User Leave

        #region Client Leave

        private async Task Commence_ClientLeave(SocketGuild Guild)
        {
            Task.Run(() => Client_ClientLeave(Guild));
        }

        private async Task Client_ClientLeave(SocketGuild Guild)
        {
            DeleteData(Guild.Id.ToString(), IgnoreCache: true, Table: "Utili_InactiveTimers");
            RunNonQuery($"DELETE FROM Utili_MessageLogs WHERE GuildID = '{Guild.Id}'");
            DeleteData(Guild.Id.ToString());
        }

        #endregion Client Leave

        #region Voice Updated

        private async Task Commence_UserVoiceStateUpdated(SocketUser UserParam, SocketVoiceState Before, SocketVoiceState After)
        {
            Task.Run(() => Client_UserVoiceStateUpdated(UserParam, Before, After));
        }

        private async Task Client_UserVoiceStateUpdated(SocketUser UserParam, SocketVoiceState Before, SocketVoiceState After)
        {
            SocketGuildUser User = UserParam as SocketGuildUser;
            VCLink VCLink = new VCLink();
            Task.Run(() => VCLink.Client_UserVoiceStateUpdated(User, Before, After));

            if (After.VoiceChannel != null)
            {
                DeleteData(User.Guild.Id.ToString(), $"InactiveRole-Timer-{User.Id}", IgnoreCache: true, Table: "Utili_InactiveTimers");
                SaveData(User.Guild.Id.ToString(), $"InactiveRole-Timer-{User.Id}", ToSQLTime(DateTime.Now), IgnoreCache: true, Table: "Utili_InactiveTimers");
            }
        }

        #endregion Voice Updated

        #region Channel Created

        private async Task Commence_ChannelCreated(SocketChannel Channel)
        {
            Task.Run(() => Client_ChannelCreated(Channel));
        }

        private async Task Client_ChannelCreated(SocketChannel Channel)
        {
            MessageLogs MessageLogs = new MessageLogs();
            Task.Run(() => MessageLogs.MessageLogs_ChannelCreated(Channel));
        }

        #endregion Channel Created

        #region Messages

        public async Task Commence_MessageDelete(Cacheable<IMessage, ulong> PartialMessage, ISocketMessageChannel Channel)
        {
            MessageLogs MessageLogs = new MessageLogs();
            Task.Run(() => MessageLogs.MessageLogs_MessageDeleted(PartialMessage, Channel));
        }

        public async Task Commence_MessageUpdated(Cacheable<IMessage, ulong> PartialMessage, SocketMessage NewMessage, ISocketMessageChannel Channel)
        {
            MessageLogs MessageLogs = new MessageLogs();
            Task.Run(() => MessageLogs.MessageLogs_MessageEdited(PartialMessage, NewMessage, Channel));
        }

        public async Task Commence_ReactionAdded(Cacheable<IUserMessage, ulong> Message, ISocketMessageChannel Channel, SocketReaction Reaction)
        {
            Votes Votes = new Votes();
            Task.Run(() => Votes.Votes_ReactionAdded(Message, Channel, Reaction));
        }

        #endregion Messages
    }
}