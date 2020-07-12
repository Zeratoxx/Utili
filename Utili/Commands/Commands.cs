using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Text;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Newtonsoft.Json;

using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;
using static Utili.Json;
using DiscordBotsList.Api;
using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient.Memcached;

namespace Utili
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("Help")]
        public async Task Help()
        {
            string Content =
                "help - Show this list\n" +
                "about - Display bot information and links\n" +
                "vote - Support the bot for free\n" +
                "prefix [prefix] - Set the bot command prefix\n" +
                "commands [on | off] [channel] - Enable or disable commands in a channel\n" +
                "deletedata - Delete all of the data that Utili has stored on your guild\n\n" +

                "prune - Delete messages\n" +
                "whohas [role] - Display a list of members with a role\n" +
                "react - React to a message\n" +
                "joinrole - Set the role to give users on join\n\n" +

                "autopurge - Manage the autopurge feature\n" +
                "logs - Manage message logging feature\n" +
                "votes - Manage the message voting feature\n" +
                "notice - Manage the channel notices feature\n" +
                "joinmessage - Manage the join message feature\n" +
                "vclink - Manage the VC Link feature\n" +
                "rolepersist - Manage the role persist feature\n" +
                "inactive - Manage the inactive role feature\n" +
                "spam - Manage the spam filter feature\n" +
                "filter - Manage the message filter feature\n" +
                "mirroring - Manage the channel mirroring feature\n" +
                "antiprofane - Manage the anti-profane filter feature";

            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Utili", Content, $"Guild Prefix: {Prefix}"));
        }

        [Command("Help")]
        public async Task Help(string Category)
        {
            string[] Categories = { "autopurge", "votes", "spam", "filter", "logs", "antiprofane", "joinrole", "prune", "inactive", "notice", "rolepersist", "vclink", "mirroring", "joinmessage" };
            if (Categories.Contains(Category.ToLower()))
            {
                string Prefix = ".";
                try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

                switch (Category.ToLower())
                {
                    case "autopurge":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Autopurge", AutopurgeCommands.HelpContent, $"Prefix these commands with {Prefix}autopurge"));
                        break;

                    case "votes":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Votes", VotesCommands.HelpContent, $"Prefix these commands with {Prefix}votes"));
                        break;

                    case "spam":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Spam Filter", SpamFilterCommands.HelpContent, $"Prefix these commands with {Prefix}spam"));
                        break;

                    case "filter":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Message Filter", FilterCommands.HelpContent, $"Prefix these commands with {Prefix}filter"));
                        break;

                    case "logs":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Message Logs", MessageLogsCommands.HelpContent, $"Prefix these commands with {Prefix}logs"));
                        break;

                    case "antiprofane":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Anti-Profane Filter", AntiprofaneCommands.HelpContent, $"Prefix these commands with {Prefix}antiprofane"));
                        break;

                    case "joinrole":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Join Role Command", UtilityCommands.JoinRoleHelpContent));
                        break;

                    case "prune":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Prune Command", UtilityCommands.PruneHelpContent));
                        break;

                    case "inactive":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Inactive Role", InactiveRoleCommands.HelpContent, $"Prefix these commands with {Prefix}inactive"));
                        break;

                    case "notice":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel Notices", NoticeMessageCommands.HelpContent, $"Prefix these commands with {Prefix}notice"));
                        break;

                    case "rolepersist":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Role Persist", RolePersistCommands.HelpContent, $"Prefix these commands with {Prefix}rolepersist"));
                        break;

                    case "vclink":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("VC Link", VCLinkCommands.HelpContent, $"Prefix these commands with {Prefix}vclink"));
                        break;

                    case "mirroring":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel Mirroring", MirroringCommands.HelpContent, $"Prefix these commands with {Prefix}mirroring"));
                        break;

                    case "joinmessage":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Join Message", JoinMessageCommands.HelpContent, $"Prefix these commands with {Prefix}joinmessage"));
                        break;
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid category"));
            }
        }

        [Command("About")]
        public async Task About()
        {
            string Content =
                "By 230Daniel#1920\n" +
                $"In {Program.Shards.Guilds.Count} servers\n" +
                $"Shard {Program.Client.ShardId + 1} of {Program.TotalShards}\n" +
                "[Support and Requests Discord](https://discord.gg/WsxqABZ)\n" +
                "[Bot Invite](https://discordapp.com/api/oauth2/authorize?client_id=655155797260501039&permissions=8&scope=bot)\n" +
                "[Github](https://github.com/D230Daniel/Utili)\n" +
                "[Donate](https://www.paypal.me/230Daniel)";
                
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed($"Utili v{Program.VersionNumber}", Content));
        }

        [Command("Prefix")]
        public async Task Prefix(string Prefix)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "Prefix");
                SaveData(Context.Guild.Id.ToString(), "Prefix", Prefix);
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"Guild prefix set to {Prefix}"));
            }
        }

        [Command("Ping"), Alias("Status")]
        public async Task Ping()
        {
            int Send = SendLatency;
            int Edit = EditLatency;
            int API = Program.Client.Latency;
            int Database = DBLatency;

            var Embed = GetLargeEmbed("Pong!", "", Footer: $"Shard { Program.Client.ShardId + 1} of { Program.TotalShards} | Shard serving {Program.Client.Guilds.Count} guilds of {Program.Shards.Guilds.Count} total.").ToEmbedBuilder();
            Embed.AddField("**Discord**", $"API: {API}ms\nSend: {Send}ms\nEdit: {Edit}ms", true);
            Embed.AddField("**Database**", $"Ping: {Database}ms\nQueries: {QueriesPerSecond}/s", true);
            Embed.AddField("**Cache**", $"\nItems: {CacheItems}\nQueries: {CacheQueriesPerSecond}/s", true);

            await Context.Channel.SendMessageAsync(embed: Embed.Build());
        }

        [Command("Vote")]
        public async Task Vote(bool Include = true)
        {
            string Content = "";
            List<Data> Data = GetData(Type: "VoteLink", IgnoreCache: true);
            Data = Data.OrderBy(x => x.ID).ToList();
            foreach(var Link in Data)
            {
                Content += $"[{Link.GuildID}]({Link.Value})\n";
            }

            List<(string, int)> TopVoters = new List<(string, int)>();
            bool Continue = true;
            int Votes = 0;

            if (Include)
            {
                try
                {
                    #region Discord Bots List (top.gg)

                    AuthDiscordBotListApi API = new AuthDiscordBotListApi(655155797260501039, Config.DiscordBotListKey);
                    var Me = await API.GetMeAsync();
                    var Voters = await Me.GetVotersAsync();

                    foreach (var Voter in Voters)
                    {
                        Votes += 1;
                        string Name = Voter.Username;
                        try { Name = Program.Shards.GetUser(Voter.Id).ToString(); } catch { }

                        if (TopVoters.FindIndex(x => x.Item1 == Name) >= 0)
                        {
                            (string, int) VoterItem = TopVoters.Find(x => x.Item1 == Name);
                            VoterItem.Item2 += 1;
                            TopVoters.RemoveAll(x => x.Item1 == Name);
                            TopVoters.Add(VoterItem);
                        }
                        else
                        {
                            (string, int) VoterItem = (Name, 1);
                            TopVoters.Add(VoterItem);
                        }
                    }

                    #endregion

                    #region Bots for Discord

                    string URL = "https://botsfordiscord.com/api/bot/655155797260501039/votes";
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
                    request.ContentType = "application/json; charset=utf-8";
                    request.Headers["Authorization"] = Config.BotsForDiscordKey;
                    request.Headers["Content-Type"] = "json";
                    request.PreAuthenticate = true;
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        StreamReader Reader = new StreamReader(responseStream, Encoding.UTF8);
                        string Str = Reader.ReadToEnd();

                        var VoterIDs = JObject.Parse(Str).SelectToken("$.repeatVotersMonth").ToList();

                        foreach (var Voter in VoterIDs)
                        {
                            Votes += 1;
                            try
                            {
                                IUser User = Program.Shards.GetUser(ulong.Parse(Voter.ToString()));
                                string Name = User.ToString();

                                if (TopVoters.FindIndex(x => x.Item1 == Name) >= 0)
                                {
                                    (string, int) VoterItem = TopVoters.Find(x => x.Item1 == Name);
                                    VoterItem.Item2 += 1;
                                    TopVoters.RemoveAll(x => x.Item1 == Name);
                                    TopVoters.Add(VoterItem);
                                }
                                else
                                {
                                    (string, int) VoterItem = (Name, 1);
                                    TopVoters.Add(VoterItem);
                                }
                            }
                            catch { }
                        }
                    }

                    #endregion
                }
                catch
                {
                    Content += "\n**Unable to fetch top voters**";
                    Continue = false;
                }

                if (Continue)
                {
                    Content += "\n**Top 5 voters this month**\n";
                    TopVoters = TopVoters.OrderBy(x => x.Item2).ToList();
                    TopVoters.Reverse();

                    int i = 0;
                    foreach (var Voter in TopVoters.Take(5))
                    {
                        i++;
                        Content += $"`{Voter.Item2}` {Voter.Item1}\n";
                    }
                }
            }
            
            if(Include) await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Vote for me!", Content, $"{Votes} votes from {TopVoters.Count} unique voters"));
            else await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Vote for me!", Content));
        }

        [Command("Commands")]
        public async Task CMDCommands(string onoff, ITextChannel Channel)
        {
            switch (onoff.ToLower())
            {
                case "on":
                    DeleteData(Context.Guild.Id.ToString(), "Commands-Disabled", Channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Commands enabled", $"Utili will now process commands sent in {Channel.Mention}"));
                    break;

                case "off":
                    SaveData(Context.Guild.Id.ToString(), "Commands-Disabled", Channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Commands disabled", $"Utili will no longer process commands sent in {Channel.Mention}"));
                    break;

                default:
                    string Prefix = ".";
                    try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {Prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
                    break;
            }
        }

        [Command("DeleteData")]
        public async Task CMDDeleteData([Remainder] string Confirm = "")
        {
            if (Context.Guild.Owner.Id == Context.User.Id)
            {
                if(Confirm.ToLower() == "confirm")
                {
                    DeleteData(Context.Guild.Id.ToString());
                    RunNonQuery($"DELETE FROM Utili_MessageLogs WHERE GuildID = '{Context.Guild.Id}'");
                    DeleteData(Context.Guild.Id.ToString(), IgnoreCache: true, Table: "Utili_InactiveTimers");

                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "All data deleted", "Utili is now storing no data on your guild."));
                }
                else
                {
                    string Prefix = ".";
                    try { Prefix = GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "WARNING", $"This command will delete **all** data that Utili has stored on {Context.Guild.Name}!\nThis includes all configuration data, all message logs, and all activity information.\n\nUse `{Prefix}deletedata confirm` to confirm this action."));
                }
            }
            else await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Permission denied", "You need to be the owner of the guild to use that command"));
        }
    }
}