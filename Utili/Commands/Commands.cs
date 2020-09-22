using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBotsList.Api;
using DiscordBotsList.Api.Objects;
using Newtonsoft.Json.Linq;
using static Utili.Data;
using static Utili.Json;
using static Utili.Logic;
using static Utili.SendMessage;

namespace Utili
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("Help")]
        public async Task Help()
        {
            string content =
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
                "vcroles - Manage the VC Roles feature\n" +
                "rolepersist - Manage the role persist feature\n" +
                "inactive - Manage the inactive role feature\n" +
                "spam - Manage the spam filter feature\n" +
                "filter - Manage the message filter feature\n" +
                "mirroring - Manage the channel mirroring feature\n" +
                "antiprofane - Manage the anti-profane filter feature";

            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Utili", content, $"Guild Prefix: {prefix}"));
        }

        [Command("Help")]
        public async Task Help(string category)
        {
            string[] categories = { "autopurge", "votes", "spam", "filter", "logs", "antiprofane", "joinrole", "prune", "inactive", "notice", "rolepersist", "vclink", "mirroring", "joinmessage" };
            if (categories.Contains(category.ToLower()))
            {
                string prefix = ".";
                try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

                switch (category.ToLower())
                {
                    case "autopurge":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Autopurge", AutopurgeCommands.HelpContent, $"Prefix these commands with {prefix}autopurge"));
                        break;

                    case "votes":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Votes", VotesCommands.HelpContent, $"Prefix these commands with {prefix}votes"));
                        break;

                    case "spam":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Spam Filter", SpamFilterCommands.HelpContent, $"Prefix these commands with {prefix}spam"));
                        break;

                    case "filter":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Message Filter", FilterCommands.HelpContent, $"Prefix these commands with {prefix}filter"));
                        break;

                    case "logs":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Message Logs", MessageLogsCommands.HelpContent, $"Prefix these commands with {prefix}logs"));
                        break;

                    case "antiprofane":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Anti-Profane Filter", AntiprofaneCommands.HelpContent, $"Prefix these commands with {prefix}antiprofane"));
                        break;

                    case "joinrole":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Join Role Command", UtilityCommands.JoinRoleHelpContent));
                        break;

                    case "prune":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Prune Command", UtilityCommands.PruneHelpContent));
                        break;

                    case "inactive":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Inactive Role", InactiveRoleCommands.HelpContent, $"Prefix these commands with {prefix}inactive"));
                        break;

                    case "notice":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel Notices", NoticeMessageCommands.HelpContent, $"Prefix these commands with {prefix}notice"));
                        break;

                    case "rolepersist":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Role Persist", RolePersistCommands.HelpContent, $"Prefix these commands with {prefix}rolepersist"));
                        break;

                    case "vclink":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("VC Link", VcLinkCommands.HelpContent, $"Prefix these commands with {prefix}vclink"));
                        break;

                    case "mirroring":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel Mirroring", MirroringCommands.HelpContent, $"Prefix these commands with {prefix}mirroring"));
                        break;

                    case "joinmessage":
                        await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Join Message", JoinMessageCommands.HelpContent, $"Prefix these commands with {prefix}joinmessage"));
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
            string content =
                "By 230Daniel#1920\n" +
                $"In {Program.Shards.Guilds.Count} servers\n" +
                $"Shard {Program.Client.ShardId + 1} of {Program.TotalShards}\n" +
                "[Support and Requests Discord](https://discord.gg/WsxqABZ)\n" +
                "[Bot Invite](https://discordapp.com/api/oauth2/authorize?client_id=655155797260501039&permissions=8&scope=bot)\n" +
                "[Github](https://github.com/D230Daniel/Utili)\n" +
                "[Donate](https://www.paypal.me/230Daniel)";

            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed($"Utili v{Program.VersionNumber}", content));
        }

        [Command("Prefix")]
        public async Task Prefix(string prefix)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "Prefix");
                SaveData(Context.Guild.Id.ToString(), "Prefix", prefix);
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"Guild prefix set to {prefix}"));
            }
        }

        [Command("Ping"), Alias("Status")]
        public async Task Ping(string details = "none")
        {
            int send = SendLatency;
            int edit = EditLatency;
            int api = Program.Client.Latency;
            int database = DbLatency;

            EmbedBuilder embed = GetLargeEmbed("Pong!", "", footer: $"Shard { Program.Client.ShardId + 1} of { Program.TotalShards} | Shard serving {Program.Client.Guilds.Count} guilds of {Program.Shards.Guilds.Count} total.").ToEmbedBuilder();
            embed.AddField("**Discord**", $"API: {api}ms\nSend: {send}ms\nEdit: {edit}ms", true);
            embed.AddField("**Database**", $"Ping: {database}ms\nQueries: {QueriesPerSecond}/s", true);
            embed.AddField("**Cache**", $"\nItems: {CacheItems}\nQueries: {CacheQueriesPerSecond}/s", true);

            if (details.ToLower() == "common" && OwnerPermission(Context.User, null))
            {
                embed.Description = $"**Most commonly requested data items**\n{CommonItemsOutput}";
            }

            if (details.ToLower() == "got" && OwnerPermission(Context.User, null))
            {
                embed.Description = $"**Most commonly gotten from database**\n{CommonItemsGotOutput}";
            }

            if (details.ToLower() == "saved" && OwnerPermission(Context.User, null))
            {
                embed.Description = $"**Most commonly saved to database**\n{CommonItemsSavedOutput}";
            }

            if (details.ToLower() == "reactions" && OwnerPermission(Context.User, null))
            {
                embed.Description = $"**Reactions added/removed:** {ReactionsAlteredPerSecond}/s\n**Max:** {MaxReactionsAlteredPerSecond}";
            }

            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }

        [Command("Vote")]
        public async Task Vote(bool include = true)
        {
            string content = "";
            List<Data> data = GetData(type: "VoteLink", ignoreCache: true);
            data = data.OrderBy(x => x.Id).ToList();
            foreach (Data link in data)
            {
                content += $"[{link.GuildId}]({link.Value})\n";
            }

            List<(string, int)> topVoters = new List<(string, int)>();
            bool @continue = true;
            int votes = 0;

            if (include)
            {
                try
                {
                    #region Discord Bots List (top.gg)

                    AuthDiscordBotListApi api = new AuthDiscordBotListApi(655155797260501039, Config.DiscordBotListKey);
                    IDblSelfBot me = await api.GetMeAsync();
                    List<IDblEntity> voters = await me.GetVotersAsync();

                    foreach (IDblEntity voter in voters)
                    {
                        votes += 1;
                        string name = voter.Username;
                        try { name = Program.Shards.GetUser(voter.Id).ToString(); } catch { }

                        if (topVoters.FindIndex(x => x.Item1 == name) >= 0)
                        {
                            (string, int) voterItem = topVoters.Find(x => x.Item1 == name);
                            voterItem.Item2 += 1;
                            topVoters.RemoveAll(x => x.Item1 == name);
                            topVoters.Add(voterItem);
                        }
                        else
                        {
                            (string, int) voterItem = (name, 1);
                            topVoters.Add(voterItem);
                        }
                    }

                    #endregion Discord Bots List (top.gg)

                    #region Bots for Discord

                    string url = "https://botsfordiscord.com/api/bot/655155797260501039/votes";
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.ContentType = "application/json; charset=utf-8";
                    request.Headers["Authorization"] = Config.BotsForDiscordKey;
                    request.Headers["Content-Type"] = "json";
                    request.PreAuthenticate = true;
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                    using Stream responseStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                    string str = reader.ReadToEnd();

                    List<JToken> voterIDs = JObject.Parse(str).SelectToken("$.repeatVotersMonth").ToList();

                    foreach (JToken voter in voterIDs)
                    {
                        votes += 1;
                        try
                        {
                            IUser user = Program.Shards.GetUser(ulong.Parse(voter.ToString()));
                            string name = user.ToString();

                            if (topVoters.FindIndex(x => x.Item1 == name) >= 0)
                            {
                                (string, int) voterItem = topVoters.Find(x => x.Item1 == name);
                                voterItem.Item2 += 1;
                                topVoters.RemoveAll(x => x.Item1 == name);
                                topVoters.Add(voterItem);
                            }
                            else
                            {
                                (string, int) voterItem = (name, 1);
                                topVoters.Add(voterItem);
                            }
                        }
                        catch { }
                    }

                    #endregion Bots for Discord
                }
                catch
                {
                    content += "\n**Unable to fetch top voters**";
                    @continue = false;
                }

                if (@continue)
                {
                    content += "\n**Top 5 voters this month**\n";
                    topVoters = topVoters.OrderBy(x => x.Item2).ToList();
                    topVoters.Reverse();

                    int i = 0;
                    foreach ((string, int) voter in topVoters.Take(5))
                    {
                        i++;
                        content += $"`{voter.Item2}` {voter.Item1}\n";
                    }
                }
            }

            if (include) await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Vote for me!", content, $"{votes} votes from {topVoters.Count} unique voters"));
            else await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Vote for me!", content));
        }

        [Command("Commands")]
        public async Task CmdCommands(string onoff, ITextChannel channel)
        {
            switch (onoff.ToLower())
            {
                case "on":
                    DeleteData(Context.Guild.Id.ToString(), "Commands-Disabled", channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Commands enabled", $"Utili will now process commands sent in {channel.Mention}"));
                    break;

                case "off":
                    SaveData(Context.Guild.Id.ToString(), "Commands-Disabled", channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Commands disabled", $"Utili will no longer process commands sent in {channel.Mention}"));
                    break;

                default:
                    string prefix = ".";
                    try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
                    break;
            }
        }

        [Command("DeleteData")]
        public async Task CmdDeleteData([Remainder] string confirm = "")
        {
            if (Context.Guild.Owner.Id == Context.User.Id)
            {
                if (confirm.ToLower() == "confirm")
                {
                    DeleteData(Context.Guild.Id.ToString());
                    RunNonQuery($"DELETE FROM Utili_MessageLogs WHERE GuildID = '{Context.Guild.Id}'");
                    DeleteData(Context.Guild.Id.ToString(), ignoreCache: true, table: "Utili_InactiveTimers");

                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "All data deleted", "Utili is now storing no data on your guild."));
                }
                else
                {
                    string prefix = ".";
                    try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "WARNING", $"This command will delete **all** data that Utili has stored on {Context.Guild.Name}!\nThis includes all configuration data, all message logs, and all activity information.\n\nUse `{prefix}deletedata confirm` to confirm this action."));
                }
            }
            else await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Permission denied", "You need to be the owner of the guild to use that command"));
        }
    }
}