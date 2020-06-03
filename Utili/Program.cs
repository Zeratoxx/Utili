﻿using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Diagnostics;

using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Collections.Generic;

using static Utili.SendMessage;
using static Utili.Data;
using static Utili.Json;

using System.Threading;
using System.Linq;
using System.Runtime.InteropServices;
using DiscordBotsList.Api;
using System.Net.Http;
using System.Net;
using Google.Apis.YouTube.v3;
using Google.Apis.Services;

namespace Utili
{
    class Program
    {
        public static string VersionNumber = "1.10.10";

        public static DiscordSocketClient Client;
        public static DiscordSocketClient GlobalClient;
        public static DiscordSocketClient ShardsClient;
        private CommandService Commands;
        public static YouTubeService Youtube;
        public static CancellationTokenSource ForceStop;
        public static int TotalShards = 0;
        public static bool Ready = false;

        public static bool Debug = false;
        public static bool UseTest = false;

        DateTime LastStatsUpdate = DateTime.Now;

        #region System

        static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) & UseTest == false)
            {
                Console.WriteLine($"WARNING:\nRunning Utili on a non-linux machine!\n\nUse the test bot if you've changed code.\nPress Y to continue...");
                if (Console.ReadKey().Key != ConsoleKey.Y) Environment.Exit(0);
                Console.Clear();
            }

            if(!Debug) Console.WriteLine("See output.txt");
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
                            if(e.InnerException == null) Console.WriteLine($"[{DateTime.Now}] [Crash] {e.Message}\n\nRestarting...\n\n");
                            else Console.WriteLine($"[{DateTime.Now}] [Crash] {e.Message}\n{e.InnerException.Message}\n\nRestarting...\n\n");
                            
                            Retry = true;
                            Thread.Sleep(5000);
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
                await GlobalClient.StopAsync();
                Client.Dispose();
                GlobalClient.Dispose();
            }
            catch { }

            int ShardID = 0;
            TotalShards = 1;

            if (!UseTest)
            {
                TotalShards = await Sharding.GetTotalShards();
                await Sharding.GetShardID();
            }
            

            if (!Debug)
            {
                Client = new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Info,
                    MessageCacheSize = 100,
                    ShardId = ShardID,
                    TotalShards = TotalShards,
                    ConnectionTimeout = 15000
                });

                StreamWriter outputFile = null;

                if (!File.Exists("Output.txt")) outputFile = File.CreateText("Output.txt");
                else outputFile = File.AppendText("Output.txt");
                outputFile.AutoFlush = true;
                Console.SetOut(outputFile);
                Console.SetError(outputFile);
            }
            else
            {
                Client = new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Info,
                    MessageCacheSize = 100,
                    ShardId = ShardID,
                    TotalShards = TotalShards,
                    ConnectionTimeout = 15000
                });
            }

            if (!UseTest)
            {
                _ = Sharding.KeepConnection();
                _ = Sharding.FlushDisconnected();
            }

            GlobalClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Critical,
                MessageCacheSize = 100
            });

            ShardsClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Critical,
                MessageCacheSize = 0
            });

            ShardsClient.Log += Client_Log;

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

            Console.WriteLine($"\n[{DateTime.Now}] [Info] Loading cache...");
            Cache = GetData(IgnoreCache: true).ToHashSet();
            Console.WriteLine($"[{DateTime.Now}] [Info] {Cache.Count} items cached");

            Console.WriteLine($"[{DateTime.Now}] [Info] Starting bot on version {VersionNumber}");

            string Token;

            if (!UseTest) Token = Config.Token;
            else Token = Config.TestToken;

            QueryTimer = DateTime.Now;
            Queries = 0;
            CacheQueries = 0;

            await Client.LoginAsync(TokenType.Bot, Token);
            await Client.StartAsync();

            await GlobalClient.LoginAsync(TokenType.Bot, Token);
            await GlobalClient.StartAsync();

            if (!UseTest)
            {
                await ShardsClient.LoginAsync(TokenType.Bot, Config.ShardsToken);
                await ShardsClient.StartAsync();

                _ = Sharding.UpdateShardMessage();
            }

            var LatencyTimer = new System.Timers.Timer(10000);
            LatencyTimer.Elapsed += UpdateLatency;
            LatencyTimer.Start();

            Autopurge Autopurge = new Autopurge();
            InactiveRole InactiveRole = new InactiveRole();

            _ = Autopurge.Run();
            _ = InactiveRole.Run();

            ForceStop = new CancellationTokenSource();

            var ReliabilityTimer = new System.Timers.Timer(5000);
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
            TimeSpan Uptime = DateTime.Now - QueryTimer;
            try { QueriesPerSecond = Math.Round(Queries / Uptime.TotalSeconds, 2); } catch { QueriesPerSecond = 0; }
            try { CacheQueriesPerSecond = Math.Round(CacheQueries / Uptime.TotalSeconds, 2); } catch { CacheQueriesPerSecond = 0; }

            try
            {
                DateTime Now = DateTime.Now;
                GetData("Ping Test", IgnoreCache: true);
                DBLatency = (int)Math.Round((DateTime.Now - Now).TotalMilliseconds);

                DatabaseItems = GetData(IgnoreCache: true).Count() + GetData(IgnoreCache: true, Table: "Utili_InactiveTimers").Count();
            }
            catch { DBLatency = -1; };

            try
            {
                DateTime Now = DateTime.Now;
                GetData("Ping Test");
                CacheLatency = (int)Math.Round((DateTime.Now - Now).TotalMilliseconds);

                CacheItems = Cache.Count;
            }
            catch { CacheLatency = -1; };

            try
            {
                DateTime Now = DateTime.Now;
                var Sent = await Client.GetGuild(682882628168450079).GetTextChannel(713125991563919492).SendMessageAsync("Testing send latency...");
                SendLatency = (int)Math.Round((DateTime.Now - Now).TotalMilliseconds);

                Now = DateTime.Now;
                await Sent.ModifyAsync(x => x.Content = "Testing edit latency...");
                EditLatency = (int)Math.Round((DateTime.Now - Now).TotalMilliseconds);

                await Sent.DeleteAsync();
            }
            catch { SendLatency = -1; };

            QueryTimer = DateTime.Now;
            Queries = 0;
            CacheQueries = 0;
        }

        private async void CheckReliability(object sender, System.Timers.ElapsedEventArgs e)
        {
            if(Client.ConnectionState != ConnectionState.Connected || Client.Latency > 10000)
            {
                try { await Task.Delay(10000, ForceStop.Token); } catch { }

                if (Client.ConnectionState != ConnectionState.Connected || Client.Latency > 10000) 
                {
                    if (ForceStop.IsCancellationRequested || !Ready)
                    {
                        return;
                    }

                    Console.WriteLine($"[{DateTime.Now}] [Info] Script terminated due to prolonged disconnect or high latency [{Client.ConnectionState} {Client.Latency}]");
                    Ready = false;
                    ForceStop.Cancel(); 
                }
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

            Youtube = new YouTubeService(new BaseClientService.Initializer()
            {
                ApplicationName = Config.Youtube.ApplicationName,
                ApiKey = Config.Youtube.Key
            });

            UpdateStats();

            AntiProfane AntiProfane = new AntiProfane();
            Task.Run(() => AntiProfane.AntiProfane_Ready());

            Ready = true;
        }

        #endregion

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

            #endregion

            #region System

            var Message = MessageParam as SocketUserMessage;
            var Context = new SocketCommandContext(Client, Message);


            #endregion

            var GuildUser = Context.User as SocketGuildUser;
            UserStatus Status = GuildUser.Status;

            #region Reject DMs

            if (Message.Channel.GetType() == typeof(SocketDMChannel) & !Context.User.IsBot)
            {
                return;
            }

            #endregion

            #region Command Handler

            if (!(Context.Message == null || Context.Message.ToString() == "" || Context.User.Id == Client.CurrentUser.Id || Context.User.IsBot))
            {
                if(GetData(Context.Guild.Id.ToString(), "Commands-Disabled", Context.Channel.Id.ToString()).Count == 0)
                {
                    int ArgPos = 0;

                    string Prefix = ".";
                    try { Prefix = GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

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

                                    #endregion
                                    #region Command Error Logging

                                    if (!File.Exists("Command Log.txt")) sw = File.CreateText("Command Log.txt");
                                    else sw = File.AppendText("Command Log.txt");

                                    sw.WriteLine($"[{DateTime.Now}] [Command] [Error] {Result.ErrorReason}");

                                    sw.Close();

                                    #endregion
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

                                #endregion
                            }
                        }
                        catch
                        {
                            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "An unexpected error occured", $"Please report this"));
                        }
                    }
                }
            }

            #endregion

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

            #endregion
        }

        #endregion

        #region Update Stats

        private async Task UpdateStats()
        {
            LastStatsUpdate = DateTime.Now;

            AuthDiscordBotListApi API = new AuthDiscordBotListApi(655155797260501039, Config.DiscordBotListKey);
            var Me = await API.GetMeAsync();
            if (Me.Id != Client.CurrentUser.Id) return;
            await Me.UpdateStatsAsync(GlobalClient.Guilds.Count);
        }

        #endregion

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

            var Role = User.Guild.GetRole(ulong.Parse(Data.GetData(User.Guild.Id.ToString(), "JoinRole").First().Value));
            await User.AddRoleAsync(Role);
        }

        #endregion

        #region Client Join

        private async Task Commence_ClientJoin(SocketGuild Guild)
        {
            Task.Run(() => Client_ClientJoin(Guild));
        }

        private async Task Client_ClientJoin(SocketGuild Guild)
        {
            DeleteData(Guild.Id.ToString(), IgnoreCache: true, Table: "Utili_InactiveTimers");
            DateTime StartTime = DateTime.Now;
            foreach(var User in Guild.Users)
            {
                SaveData(User.Guild.Id.ToString(), $"InactiveRole-Timer-{User.Id}", ToSQLTime(StartTime), IgnoreCache: true, Table: "Utili_InactiveTimers");
                await Task.Delay(100);
            }
        }

        #endregion

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
        #endregion

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

        #endregion

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
        }

        #endregion

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

        #endregion

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

        #endregion
    }
}
