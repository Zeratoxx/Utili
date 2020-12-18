using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using static Utili.Data;
using static Utili.InactiveRole;
using static Utili.Logic;
using static Utili.Program;
using static Utili.SendMessage;

namespace Utili
{
    internal class InactiveRole
    {
        public static List<Task> Tasks = new List<Task>();

        public static Timer StartRunthrough;

        public async Task InactiveRole_MessageReceived(SocketMessage messageParam)
        {
            SocketUserMessage message = messageParam as SocketUserMessage;
            SocketCommandContext context = new SocketCommandContext(_client, message);

            if (context.User.IsBot) return;

            SocketGuildUser usr = context.User as SocketGuildUser;

            if(!GetPerms(context.Guild).ManageRoles) return;

            Data inactiveRole = GetFirstData(context.Guild.Id.ToString(), "InactiveRole-Role");
            if (inactiveRole != null)
            {
                SocketRole role = context.Guild.GetRole(ulong.Parse(inactiveRole.Value));

                if (usr.Roles.Select(x => x.Id).Contains(role.Id))
                {
                    await usr.RemoveRoleAsync(role);
                }

                int rowsAffected = RunNonQuery("UPDATE Utili_InactiveTimers SET DataValue = @Value WHERE GuildID = @GuildID AND DataType = @Type", new[] { ("GuildID", context.Guild.Id.ToString()), ("Type", $"InactiveRole-Timer-{usr.Id}"), ("Value", ToSqlTime(DateTime.Now)) });
                if (rowsAffected == 0)
                {
                    SaveData(context.Guild.Id.ToString(), $"InactiveRole-Timer-{usr.Id}", ToSqlTime(DateTime.Now), ignoreCache: true, table: "Utili_InactiveTimers");
                }
            }
        }

        public async Task Run()
        {
            StartRunthrough = new Timer(120000);
            StartRunthrough.Elapsed += StartRunthrough_Elapsed;
            StartRunthrough.Start();
        }

        private void StartRunthrough_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Ready)
            {
                Tasks.RemoveAll(x => x.IsCompleted || x.IsCanceled || x.IsFaulted);
                if (Tasks.Count == 0) Tasks.Add(ProcessAll());
            }
        }

        public async Task ProcessAll()
        {
            Console.WriteLine($"{DateTime.Now} [Debug] Started inactive role timer");
            List<Data> all = GetData(type: "InactiveRole-Role");
            List<ulong> allGuilds = new List<ulong>();

            foreach (Data data in all)
            {
                if (!allGuilds.Contains(ulong.Parse(data.GuildId))) allGuilds.Add(ulong.Parse(data.GuildId));
            }

            foreach (ulong guildId in allGuilds)
            {
                try
                {
                    ulong roleId = ulong.Parse(GetFirstData(guildId.ToString(), "InactiveRole-Role").Value);
                    await ProcessGuild(guildId, roleId, false);

                    await Task.Delay(1000);
                }
                catch { }
            }
        }

        public static async Task ProcessGuild(ulong guildId, ulong roleId, bool depth = false)
        {
            SocketGuild guild = _client.GetGuild(guildId);
            SocketRole role = guild.GetRole(roleId);

            if(!GetPerms(guild).ManageRoles) return;
            if(role.Position >= guild.GetUser(_client.CurrentUser.Id).Roles.OrderBy(x => x.Position).Last().Position) return;

            SocketRole immuneRole = null;
            try { immuneRole = guild.GetRole(ulong.Parse(GetFirstData(guild.Id.ToString(), "InactiveRole-ImmuneRole").Value)); }
            catch { }

            TimeSpan threshold;
            try { threshold = TimeSpan.Parse(GetFirstData(guild.Id.ToString(), "InactiveRole-Timespan").Value); }
            catch { threshold = TimeSpan.FromDays(30); }

            string mode = "Give";
            try { mode = GetFirstData(guild.Id.ToString(), "InactiveRole-Mode").Value; }
            catch { }

            DateTime defaultTime = DateTime.MinValue;
            try { defaultTime = guild.GetUser(_client.CurrentUser.Id).JoinedAt.Value.LocalDateTime; } catch { }

            List<Data> activityData = GetDataList(guild.Id.ToString(), ignoreCache: true, table: "Utili_InactiveTimers");

            foreach (SocketGuildUser user in guild.Users)
            {
                if (!user.IsBot)
                {
                    try
                    {
                        bool hasRole = user.Roles.Contains(role);
                        if (mode == "Take") hasRole = !hasRole;

                        bool inactive = false;
                        DateTime lastThing = defaultTime;

                        try { lastThing = DateTime.Parse(activityData.First(x => x.Type == $"InactiveRole-Timer-{user.Id}").Value); CacheQueries += 1; }
                        catch { }

                        if (!hasRole) //Inversed if other mode selected
                        {
                            if (DateTime.Now - lastThing > threshold) inactive = true;

                            if (immuneRole != null && user.Roles.Any(x => x.Id == immuneRole.Id)) inactive = false;

                            if (inactive && mode == "Give") await user.AddRoleAsync(role);
                            if (inactive && mode == "Take") await user.RemoveRoleAsync(role);

                            await Task.Delay(1000);
                        }
                        else
                        {
                            inactive = true;
                            if (depth)
                            {
                                if (DateTime.Now - lastThing < threshold) inactive = false;

                                if (immuneRole != null && user.Roles.Any(x => x.Id == immuneRole.Id)) inactive = false;

                                if (!inactive && mode == "Give") await user.RemoveRoleAsync(role);
                                if (!inactive && mode == "Take") await user.AddRoleAsync(role);

                                await Task.Delay(1000);
                            }
                        }
                    }
                    catch { }
                }
            }
        }
    }

    [Group("Inactive")]
    public class InactiveRoleCommands : ModuleBase<SocketCommandContext>
    {
        public static string HelpContent =
                "help - Show this list\n" +
                "about - Display feature information\n" +
                "role [role | none] - Set the role to be given to or taken from inactive users\n" +
                "time [timespan] - Set the time threshold for inactivity\n" +
                "immunerole [role | none] - Users with this role will not be marked as inactive\n" +
                "mode [give | take] - Set whether the role is given to or taken from inactive users\n" +
                "list [page] - Lists inactive users\n" +
                "kick - Kick all inactive users";

        [Command("Help")]
        public async Task Help()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Inactive Role", HelpContent, $"Prefix these commands with {prefix}inactive"));
        }

        [Command("")]
        public async Task Empty()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Inactive Role", HelpContent, $"Prefix these commands with {prefix}inactive"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Inactive Role", "If an inactive role is set, users that are inactive for a specific amount of time are given the role automatically.\nThe following events reset the inactivity timer of a user:\n - Sending a message\n - Connecting to a voice channel\n - Joining the guild\nThe inactivity timer of all users is reset when the bot joins the guild.\nActivity data is recorded regardless of whether a role is set or not."));
        }

        [Command("Role"), Alias("Setrole", "Set")]
        public async Task Role(IRole role)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "InactiveRole-Role");
                SaveData(Context.Guild.Id.ToString(), "InactiveRole-Role", role.Id.ToString());
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set inactive role", $"Inactive users will be given the {role.Mention} role\nYour users are now being processed, this may take a while."));
                ulong roleId = ulong.Parse(GetFirstData(Context.Guild.Id.ToString(), "InactiveRole-Role").Value);
                await ProcessGuild(Context.Guild.Id, roleId, true);
            }
        }

        [Command("Role"), Alias("Setrole", "Set")]
        public async Task Role(string none)
        {
            if (none.ToLower() == "none")
            {
                if (Permission(Context.User, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "InactiveRole-Role");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Removed inactive role", "Users currently with this role won't have it removed from them"));
                }
            }
            else
            {
                string prefix = ".";
                try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
            }
        }

        [Command("ImmuneRole")]
        public async Task ImmuneRole(IRole role)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "InactiveRole-ImmuneRole");
                SaveData(Context.Guild.Id.ToString(), "InactiveRole-ImmuneRole", role.Id.ToString());
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set immune role", $"Users with the {role.Mention} role will never be marked as inactive.\nYour users are now being processed, this may take a while."));
                ulong roleId = ulong.Parse(GetFirstData(Context.Guild.Id.ToString(), "InactiveRole-Role").Value);
                await ProcessGuild(Context.Guild.Id, roleId, true);
            }
        }

        [Command("ImmuneRole"), Alias("Setrole", "Set")]
        public async Task ImmuneRole(string none)
        {
            if (none.ToLower() == "none")
            {
                if (Permission(Context.User, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "InactiveRole-ImmuneRole");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Removed immune role", "All users can now be marked as inactive.\nYour users are now being processed, this may take a while."));
                }
            }
            else
            {
                string prefix = ".";
                try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
            }
        }

        [Command("Time"), Alias("Timespan")]
        public async Task Time(TimeSpan time)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "InactiveRole-Timespan");
                SaveData(Context.Guild.Id.ToString(), "InactiveRole-Timespan", time.ToString());
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set inactive timer", $"After {DisplayTimespan(time)} of inactivity users will get the role.\nYour users are now being processed, this may take a while."));
                ulong roleId = ulong.Parse(GetFirstData(Context.Guild.Id.ToString(), "InactiveRole-Role").Value);
                await ProcessGuild(Context.Guild.Id, roleId, true);
            }
        }

        [Command("Mode")]
        public async Task Mode(string mode)
        {
            if (mode.ToLower() == "give")
            {
                if (Permission(Context.User, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "InactiveRole-Mode");
                    SaveData(Context.Guild.Id.ToString(), "InactiveRole-Mode", "Give");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Mode set", "Inactive users will now be given the role and active users will have it taken away from them.\nYour users are now being processed, this may take a while."));

                    ulong roleId = ulong.Parse(GetFirstData(Context.Guild.Id.ToString(), "InactiveRole-Role").Value);
                    await ProcessGuild(Context.Guild.Id, roleId, true);
                }
            }
            else if (mode.ToLower() == "take")
            {
                if (Permission(Context.User, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "InactiveRole-Mode");
                    SaveData(Context.Guild.Id.ToString(), "InactiveRole-Mode", "Take");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Mode set", "Active users will now be given the role and inactive users will have it taken away from them.\nYour users are now being processed, this may take a while."));

                    ulong roleId = ulong.Parse(GetFirstData(Context.Guild.Id.ToString(), "InactiveRole-Role").Value);
                    await ProcessGuild(Context.Guild.Id, roleId, true);
                }
            }
            else
            {
                string prefix = ".";
                try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
            }
        }

        [Command("List")]
        public async Task List(int page = 1)
        {
            if (page < 1) { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid page")); return; }
            page -= 1;

            IRole role;
            try { role = Context.Guild.GetRole(ulong.Parse(GetFirstData(Context.Guild.Id.ToString(), "InactiveRole-Role").Value)); }
            catch { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "No inactive role is set")); return; }

            string mode = "Give";
            try { mode = GetFirstData(Context.Guild.Id.ToString(), "InactiveRole-Mode").Value; }
            catch { }

            List<SocketGuildUser> inactiveUsers = null;
            if (mode == "Give") inactiveUsers = Context.Guild.Users.Where(x => x.Roles.Contains(role)).Where(x => !x.IsBot).OrderBy(x => x.JoinedAt).ToList();
            else inactiveUsers = Context.Guild.Users.Where(x => !x.Roles.Contains(role)).Where(x => !x.IsBot).OrderBy(x => x.JoinedAt).ToList();

            string content = "";
            int i = 1;
            i *= page;
            foreach (SocketGuildUser user in inactiveUsers.Skip(page * 50).Take(50))
            {
                content += $"{user.Mention}\n";
            }

            decimal temp = inactiveUsers.Count();
            decimal temp2 = temp / 50m;
            decimal pages = Math.Ceiling(temp2);

            if (page + 1 > pages)
            {
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid page"));
            }
            else
            {
                await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Inactive Users", content, $"Page {page + 1} of {pages}"));
            }
        }

        [Command("Kick")]
        public async Task Kick()
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(Context.Guild, new[] { GuildPermission.KickMembers }, Context.Channel))
                {
                    IRole role;
                    try { role = Context.Guild.GetRole(ulong.Parse(GetFirstData(Context.Guild.Id.ToString(), "InactiveRole-Role").Value)); }
                    catch { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "No inactive role is set")); return; }

                    string mode = "Give";
                    try { mode = GetFirstData(Context.Guild.Id.ToString(), "InactiveRole-Mode").Value; }
                    catch { }

                    List<SocketGuildUser> inactiveUsers = null;
                    if (mode == "Give") inactiveUsers = Context.Guild.Users.Where(x => x.Roles.Contains(role)).Where(x => !x.IsBot).OrderBy(x => x.JoinedAt).ToList();
                    else inactiveUsers = Context.Guild.Users.Where(x => !x.Roles.Contains(role)).Where(x => !x.IsBot).OrderBy(x => x.JoinedAt).ToList();

                    if (mode == "Give") await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Kicking inactive users", $"Kicking {inactiveUsers.Count} users with role {role.Mention}"));
                    else await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Kicking inactive users", $"Kicking {inactiveUsers.Count} users without the role {role.Mention}"));

                    foreach (SocketGuildUser user in inactiveUsers)
                    {
                        try { _ = user.KickAsync($"Mass kick for inactivity executed by {Context.User.Username}#{Context.User.Discriminator} ({Context.User.Id})"); }
                        catch { }
                    }
                }
            }
        }
    }
}