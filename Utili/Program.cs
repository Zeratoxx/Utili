using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

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
        public static string VersionNumber = "1.10.1";

        public static DiscordSocketClient Client;
        public static DiscordSocketClient GlobalClient;
        public static DiscordSocketClient ShardsClient;
        private CommandService Commands;
        public static YouTubeService Youtube;
        public static CancellationTokenSource ForceStop;
        public static int TotalShards = 0;

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

            Console.WriteLine("See output.txt");
            bool Retry = true;
            while (true)
            {
                try
                {
                    if (Retry)
                    {
                        Retry = false;
                        ForceStop = new CancellationTokenSource();
                        new Program().MainAsync().GetAwaiter().GetResult();
                    }
                }
                catch (Exception e)
                {
                    try
                    {
                        if (Client.ConnectionState == ConnectionState.Disconnected)
                        {
                            Console.WriteLine($"[{DateTime.Now}] [Crash] {e.Message}\n{e.InnerException.Message}\n\nRestarting...\n\n");
                            
                            Retry = true;
                            Thread.Sleep(10000);
                        }
                        else Console.WriteLine($"[{DateTime.Now}] [Exception] {e.Message}");
                    }
                    catch //Only if Client.ConnectionState errors
                    {
                        Console.WriteLine($"[{DateTime.Now}] [Crash] {e.Message}\n\nRestarting...\n");
                        Retry = true;
                        Thread.Sleep(10000);
                    }
                }
            }
        }

        private async Task MainAsync()
        {
            if (!LoadConfig()) GenerateNewConfig();
            SetConnectionString();

            try
            {
                await Client.StopAsync();
                await GlobalClient.StopAsync();
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
                    LogLevel = LogSeverity.Debug,
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
            Client.JoinedGuild += Commence_JoinedGuild;
            Client.MessageDeleted += Commence_MessageDelete;
            Client.MessageUpdated += Commence_MessageUpdated;
            Client.UserJoined += Commence_UserJoin;
            Client.UserLeft += Commence_UserLeft;
            Client.UserVoiceStateUpdated += Commence_UserVoiceStateUpdated;
            Client.ChannelCreated += Commence_ChannelCreated;
            Client.ReactionAdded += Commence_ReactionAdded;

            Commands.AddTypeReader(typeof(TimeSpan), new TimespanTypeReader());

            await Commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);

            Client.Ready += Client_Ready;
            Client.Log += Client_Log;

            Console.WriteLine($"\n\n[{DateTime.Now}] [Info] Starting bot on version {VersionNumber}");

            string Token;

            if (!UseTest) Token = Config.Token;
            else Token = Config.TestToken;
            
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

            Autopurge Autopurge = new Autopurge();
            InactiveRole InactiveRole = new InactiveRole();

            CancellationToken cancelAutos = new CancellationToken();
            _ = Autopurge.Run(cancelAutos);
            _ = InactiveRole.Run(cancelAutos);

            var Timer = new System.Timers.Timer(30000);
            Timer.Elapsed += CheckReliability;
            Timer.Start();

            ForceStop = new CancellationTokenSource();
            await Task.Delay(-1, ForceStop.Token);
        }

        private async void CheckReliability(object sender, System.Timers.ElapsedEventArgs e)
        {
            if(Client.ConnectionState != ConnectionState.Connected)
            {
                await Task.Delay(20000);
                if (Client.ConnectionState != ConnectionState.Connected) 
                {
                    Console.WriteLine($"[{DateTime.Now}] [Info] Detected client in a crashed state: Script terminated");
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

            #region Blacklist

            foreach (var Guild in Client.Guilds)
            {
                if (Data.GetData(Guild.Id.ToString(), "Blacklist").Count > 0)
                {
                    SocketGuildUser GuildOwner = Guild.Owner;

                    try
                    {
                        await GuildOwner.SendMessageAsync(embed: GetEmbed("No", "Guild blacklisted", $"{Guild.Name} was previously flagged as a malicious guild and is permanently blacklisted.\n" +
                            $"You can not re-invite the bot to this guild until it has been removed from the blacklist.\n" +
                            $"[Support Discord](https://discord.gg/WsxqABZ)"));
                    }
                    catch
                    {
                        SocketTextChannel DefaultChannel = Guild.DefaultChannel;

                        await DefaultChannel.SendMessageAsync($"{GuildOwner.Mention}, I couldn't DM you this message.", embed: GetEmbed("No", "Guild blacklisted", $"{Guild.Name} was previously flagged as a malicious guild and is permanently blacklisted.\n" +
                            $"You can not re-invite the bot to this guild until it has been removed from the blacklist.\n" +
                            $"[Support Discord](https://discord.gg/WsxqABZ)"));
                    }

                    Console.WriteLine($"[{DateTime.Now}] [Blacklist] Left {Guild.Name} as it is blacklisted.");
                    await Guild.LeaveAsync();
                }
            }

            #endregion

            AntiProfane AntiProfane = new AntiProfane();
            Task.Run(() => AntiProfane.AntiProfane_Ready());
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
                    try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

                    if (UseTest) Prefix = "-";

                    if (Message.HasStringPrefix(Prefix, ref ArgPos) || Message.HasMentionPrefix(Client.CurrentUser, ref ArgPos))
                    {

                        try
                        {
                            #region Command Logging

                            StreamWriter sw;
                            if (!File.Exists("Command Log.txt")) sw = File.CreateText("Command Log.txt");
                            else sw = File.AppendText("Command Log.txt");

                            sw.WriteLine($"[{DateTime.Now}] [Command] [{Context.Guild.Name} | {Context.Guild.Id}] [{Context.Channel.Name} | {Context.Channel.Id}] [{Context.User} | {Context.User.Id}] {Context.Message.Content}");

                            sw.Close();

                            #endregion

                            var Result = await Commands.ExecuteAsync(Context, ArgPos, null);

                            if (!Result.IsSuccess)
                            {
                                if (Result.ErrorReason != "Unknown command.")
                                {
                                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {Prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
                                }
                                #region Command Logging

                                if (!File.Exists("Command Log.txt")) sw = File.CreateText("Command Log.txt");
                                else sw = File.AppendText("Command Log.txt");

                                sw.WriteLine($"[{DateTime.Now}] [Command] [Error] {Result.ErrorReason}");

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

            if (Data.GetData(Context.Guild.Id.ToString()).Count() != 0)
            {
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

                InactiveRole InactiveRole = new InactiveRole();
                Task.Run(() => InactiveRole.InactiveRole_MessageReceived(MessageParam));

                NoticeMessage NoticeMessage = new NoticeMessage();
                Task.Run(() => NoticeMessage.NoticeMessage_MessageReceived(MessageParam));

                Mirroring Mirroring = new Mirroring();
                Task.Run(() => Mirroring.Mirroring_MessageReceived(MessageParam));

                if(LastStatsUpdate < DateTime.Now.AddMinutes(-1)) Task.Run(() => UpdateStats());
            }

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

        #region Join Guild
        private async Task Commence_JoinedGuild(SocketGuild Guild)
        {
            _ = Client_JoinedGuild(Guild);
        }

        private async Task Client_JoinedGuild(SocketGuild Guild)
        {
            if (Data.GetData(Guild.Id.ToString(), "Blacklist").Count > 0)
            {
                SocketGuildUser GuildOwner = Guild.Owner;

                try
                {
                    await GuildOwner.SendMessageAsync(embed: GetEmbed("No", "Guild blacklisted", $"{Guild.Name} was previously flagged as a malicious guild and is permanently blacklisted.\n" +
                        $"You can not re-invite the bot to this guild until it has been removed from the blacklist.\n" +
                        $"[Support Discord](https://discord.gg/WsxqABZ)"));
                }
                catch
                {
                    SocketTextChannel DefaultChannel = Guild.DefaultChannel;

                    await DefaultChannel.SendMessageAsync($"{GuildOwner.Mention}, I couldn't DM you this message.", embed: GetEmbed("No", "Guild blacklisted", $"{Guild.Name} was previously flagged as a malicious guild and is permanently blacklisted.\n" +
                        $"You can not re-invite the bot to this guild until it has been removed from the blacklist.\n" +
                        $"[Support Discord](https://discord.gg/WsxqABZ)"));
                }

                Console.WriteLine($"[{DateTime.Now}] [Blacklist] Left {Guild.Name} as it is blacklisted.");
                await Guild.LeaveAsync();
            }

            foreach(var User in Guild.Users)
            {
                DeleteData(Guild.Id.ToString(), $"InactiveRole-Timer-{User.Id}");
                SaveData(Guild.Id.ToString(), $"InactiveRole-Timer-{User.Id}", ToSQLTime(DateTime.Now));

                await Task.Delay(333);
            }
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

            DeleteData(User.Guild.Id.ToString(), $"InactiveRole-Timer-{User.Id}");
            SaveData(User.Guild.Id.ToString(), $"InactiveRole-Timer-{User.Id}", ToSQLTime(DateTime.Now));

            var Role = User.Guild.GetRole(ulong.Parse(Data.GetData(User.Guild.Id.ToString(), "JoinRole").First().Value));
            await User.AddRoleAsync(Role);
        }

        #endregion

        #region User Leave

        private async Task Commence_UserLeft(SocketGuildUser User)
        {
            Task.Run(() => Client_UserLeft(User));
        }

        private async Task Client_UserLeft(SocketGuildUser User)
        {
            RolePersist RolePersist = new RolePersist();
            Task.Run(() => RolePersist.UserLeft(User));
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
