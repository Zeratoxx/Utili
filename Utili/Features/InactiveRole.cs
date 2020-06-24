﻿using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Threading;
using System.Text;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;
using static Utili.Program;
using static Utili.InactiveRole;
using static Utili.Json;
using System.Timers;
using System.Runtime.ExceptionServices;

namespace Utili
{
    class InactiveRole
    {
        public static List<Task> Tasks = new List<Task>();

        public static System.Timers.Timer StartRunthrough;

        public async Task InactiveRole_MessageReceived(SocketMessage MessageParam)
        {
            var Message = MessageParam as SocketUserMessage;
            var Context = new SocketCommandContext(Client, Message);

            if (Context.User.IsBot) return;

            var Usr = Context.User as SocketGuildUser;

            //Save the data anyway so that if it's ever enabled the bot doesn't mark everyone

            int RowsAffected = RunNonQuery($"UPDATE Utili_InactiveTimers SET DataValue = @Value WHERE GuildID = @GuildID AND DataType = @Type", new (string, string)[] { ("GuildID", Context.Guild.Id.ToString()), ("Type", $"InactiveRole-Timer-{Usr.Id}"), ("Value", ToSQLTime(DateTime.Now)) });
            if(RowsAffected == 0)
            {
                SaveData(Context.Guild.Id.ToString(), $"InactiveRole-Timer-{Usr.Id}", ToSQLTime(DateTime.Now), IgnoreCache: true, Table: "Utili_InactiveTimers");
            }

            List<Data> InactiveRole = GetData(Context.Guild.Id.ToString(), "InactiveRole-Role");
            if (InactiveRole.Count != 0)
            {
                var Role = Context.Guild.GetRole(ulong.Parse(InactiveRole.First().Value));

                if (Usr.Roles.Contains(Role))
                {
                    await Usr.RemoveRoleAsync(Role);
                }
            }
        }

        public async Task Run()
        {
            StartRunthrough = new System.Timers.Timer(60000);
            StartRunthrough.Elapsed += StartRunthrough_Elapsed;
            StartRunthrough.Start();
        }

        private void StartRunthrough_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Ready)
            {
                Tasks.RemoveAll(x => x.IsCompleted);
                if (Tasks.Count <= 1) Tasks.Add(ProcessAll());
            }
        }

        public async Task ProcessAll()
        {
            List<Data> All = GetData(Type: "InactiveRole-Role");
            List<ulong> AllGuilds = new List<ulong>();

            foreach (Data Data in All)
            {
                if (!AllGuilds.Contains(ulong.Parse(Data.GuildID))) AllGuilds.Add(ulong.Parse(Data.GuildID));
            }

            foreach (ulong GuildID in AllGuilds)
            {
                try
                {
                    ulong RoleID = ulong.Parse(GetData(GuildID.ToString(), "InactiveRole-Role").First().Value);
                    await ProcessGuild(GuildID, RoleID, true);
                }
                catch { }
            }
        }

        public static async Task ProcessGuild(ulong GuildID, ulong RoleID, bool Depth = false)
        {
            SocketGuild Guild = Client.GetGuild(GuildID);
            SocketRole Role = Guild.GetRole(RoleID);

            SocketRole ImmuneRole = null;
            try { ImmuneRole = Guild.GetRole(ulong.Parse(GetData(Guild.Id.ToString(), "InactiveRole-ImmuneRole").First().Value)); }
            catch { return; };

            TimeSpan Threshold;
            try { Threshold = TimeSpan.Parse(GetData(Guild.Id.ToString(), "InactiveRole-Timespan").First().Value); }
            catch { Threshold = TimeSpan.FromDays(30); }

            string Mode = "Give";
            try { Mode = GetData(Guild.Id.ToString(), "InactiveRole-Mode").First().Value; }
            catch {}

            HashSet<Data> ActivityData = GetDataHashSet(Guild.Id.ToString(), IgnoreCache: true, Table: "Utili_InactiveTimers");

            foreach (var User in Guild.Users)
            {
                if (!User.IsBot)
                {
                    try
                    {
                        bool HasRole = User.Roles.Contains(Role);
                        if (Mode == "Take") HasRole = !HasRole;

                        bool ChangeRoles = true;
                        bool Inactive = false;
                        DateTime LastThing = DateTime.MinValue;

                        try { LastThing = DateTime.Parse(ActivityData.Where(x => x.Type == $"InactiveRole-Timer-{User.Id}").First().Value); CacheQueries += 1; }
                        catch { ChangeRoles = false; }

                        await Task.Delay(200);
                        // Simulate normal database latency as to not overload the cpu.

                        if (ChangeRoles)
                        {
                            if (!HasRole) //Inversed if other mode selected
                            {
                                if (DateTime.Now - LastThing > Threshold) Inactive = true;

                                if (ImmuneRole != null) if (User.Roles.Contains(ImmuneRole)) Inactive = false;

                                if (Inactive && Mode == "Give") _ = User.AddRoleAsync(Role);
                                if (Inactive && Mode == "Take") _ = User.RemoveRoleAsync(Role);
                            }
                            else
                            {
                                Inactive = true;
                                if (Depth)
                                {
                                    if (DateTime.Now - LastThing < Threshold) Inactive = false;

                                    if (ImmuneRole != null) if (User.Roles.Contains(ImmuneRole)) Inactive = false;

                                    if (!Inactive && Mode == "Give") _ = User.RemoveRoleAsync(Role);
                                    if (!Inactive && Mode == "Take") _ = User.AddRoleAsync(Role);
                                }
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
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Inactive Role", HelpContent, $"Prefix these commands with {Prefix}inactive"));
        }

        [Command("")]
        public async Task Empty()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Inactive Role", HelpContent, $"Prefix these commands with {Prefix}inactive"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Inactive Role", "If an inactive role is set, users that are inactive for a specific amount of time are given the role automatically.\nThe following events reset the inactivity timer of a user:\n - Sending a message\n - Connecting to a voice channel\n - Joining the guild\nThe inactivity timer of all users is reset when the bot joins the guild.\nActivity data is recorded regardless of whether a role is set or not."));
        }

        [Command("Role"), Alias("Setrole", "Set")]
        public async Task Role(IRole Role)
        {
            if(Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "InactiveRole-Role");
                SaveData(Context.Guild.Id.ToString(), "InactiveRole-Role", Role.Id.ToString());
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set inactive role", $"Inactive users will be given the {Role.Mention} role\nYour users are now being processed, this may take a while."));
                ulong RoleID = ulong.Parse(GetData(Context.Guild.Id.ToString(), "InactiveRole-Role").First().Value);
                await ProcessGuild(Context.Guild.Id, RoleID, true);
            }
        }

        [Command("Role"), Alias("Setrole", "Set")]
        public async Task Role(string None)
        {
            if(None.ToLower() == "none")
            {
                if (Permission(Context.User, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "InactiveRole-Role");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Removed inactive role", $"Users currently with this role won't have it removed from them"));
                }
            }
            else
            {
                string Prefix = ".";
                try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {Prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
            }
        }

        [Command("ImmuneRole")]
        public async Task ImmuneRole(IRole Role)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "InactiveRole-ImmuneRole");
                SaveData(Context.Guild.Id.ToString(), "InactiveRole-ImmuneRole", Role.Id.ToString());
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set immune role", $"Users with the {Role.Mention} role will never be marked as inactive.\nYour users are now being processed, this may take a while."));
                ulong RoleID = ulong.Parse(GetData(Context.Guild.Id.ToString(), "InactiveRole-Role").First().Value);
                await ProcessGuild(Context.Guild.Id, RoleID, true);
            }
        }

        [Command("ImmuneRole"), Alias("Setrole", "Set")]
        public async Task ImmuneRole(string None)
        {
            if (None.ToLower() == "none")
            {
                if (Permission(Context.User, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "InactiveRole-ImmuneRole");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Removed immune role", $"All users can now be marked as inactive.\nYour users are now being processed, this may take a while."));
                }
            }
            else
            {
                string Prefix = ".";
                try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {Prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
            }
        }

        [Command("Time"), Alias("Timespan")]
        public async Task Time(TimeSpan Time)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "InactiveRole-Timespan");
                SaveData(Context.Guild.Id.ToString(), "InactiveRole-Timespan", Time.ToString());
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set inactive timer", $"After {DisplayTimespan(Time)} of inactivity users will get the role.\nYour users are now being processed, this may take a while."));
                ulong RoleID = ulong.Parse(GetData(Context.Guild.Id.ToString(), "InactiveRole-Role").First().Value);
                await ProcessGuild(Context.Guild.Id, RoleID, true);
            }

        }

        [Command("Mode")]
        public async Task Mode(string Mode)
        {
            if (Mode.ToLower() == "give")
            {
                if (Permission(Context.User, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "InactiveRole-Mode");
                    SaveData(Context.Guild.Id.ToString(), "InactiveRole-Mode", "Give");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Mode set", $"Inactive users will now be given the role and active users will have it taken away from them.\nYour users are now being processed, this may take a while."));

                    ulong RoleID = ulong.Parse(GetData(Context.Guild.Id.ToString(), "InactiveRole-Role").First().Value);
                    await ProcessGuild(Context.Guild.Id, RoleID, true);
                }
            }
            else if (Mode.ToLower() == "take")
            {
                if(Permission(Context.User, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "InactiveRole-Mode");
                    SaveData(Context.Guild.Id.ToString(), "InactiveRole-Mode", "Take");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Mode set", $"Active users will now be given the role and inactive users will have it taken away from them.\nYour users are now being processed, this may take a while."));

                    ulong RoleID = ulong.Parse(GetData(Context.Guild.Id.ToString(), "InactiveRole-Role").First().Value);
                    await ProcessGuild(Context.Guild.Id, RoleID, true);
                }
            }
            else
            {
                string Prefix = ".";
                try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {Prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
            }
        }

        [Command("List")]
        public async Task List(int Page = 1)
        {
            if (Page < 1) { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid page")); return; }
            Page -= 1;

            IRole Role;
            try { Role = Context.Guild.GetRole(ulong.Parse(GetData(Context.Guild.Id.ToString(), "InactiveRole-Role").First().Value)); }
            catch { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "No inactive role is set")); return; }

            string Mode = "Give";
            try { Mode = GetData(Context.Guild.Id.ToString(), "InactiveRole-Mode").First().Value; }
            catch { }

            List<SocketGuildUser> InactiveUsers = null;
            if(Mode == "Give") InactiveUsers = Context.Guild.Users.Where(x => x.Roles.Contains(Role)).Where(x => !x.IsBot).OrderBy(x => x.JoinedAt).ToList();
            else InactiveUsers = Context.Guild.Users.Where(x => !x.Roles.Contains(Role)).Where(x => !x.IsBot).OrderBy(x => x.JoinedAt).ToList();

            string Content = "";
            int i = 1;
            i = i * Page;
            foreach(var User in InactiveUsers.Skip(Page * 50).Take(50))
            {
                Content += $"{User.Mention}\n";
            }

            decimal temp = InactiveUsers.Count();
            decimal temp2 = temp / 50m;
            decimal Pages = Math.Ceiling(temp2);

            if(Page + 1 > Pages)
            {
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid page"));
            }
            else
            {
                await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Inactive Users", Content, $"Page {Page + 1} of {Pages}"));
            }
        }

        [Command("Kick")]
        public async Task Kick()
        {
            if(Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(Context.Guild, new GuildPermission[] { GuildPermission.KickMembers }, Context.Channel))
                {
                    IRole Role;
                    try { Role = Context.Guild.GetRole(ulong.Parse(GetData(Context.Guild.Id.ToString(), "InactiveRole-Role").First().Value)); }
                    catch { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "No inactive role is set")); return; }

                    string Mode = "Give";
                    try { Mode = GetData(Context.Guild.Id.ToString(), "InactiveRole-Mode").First().Value; }
                    catch { }

                    List<SocketGuildUser> InactiveUsers = null;
                    if (Mode == "Give") InactiveUsers = Context.Guild.Users.Where(x => x.Roles.Contains(Role)).Where(x => !x.IsBot).OrderBy(x => x.JoinedAt).ToList();
                    else InactiveUsers = Context.Guild.Users.Where(x => !x.Roles.Contains(Role)).Where(x => !x.IsBot).OrderBy(x => x.JoinedAt).ToList();

                    if(Mode == "Give") await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Kicking inactive users", $"Kicking {InactiveUsers.Count} users with role {Role.Mention}"));
                    else await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Kicking inactive users", $"Kicking {InactiveUsers.Count} users without the role {Role.Mention}"));

                    foreach (var User in InactiveUsers)
                    {
                        try { _ = User.KickAsync($"Mass kick for inactivity executed by {Context.User.Username}#{Context.User.Discriminator} ({Context.User.Id})"); }
                        catch { }
                    }
                }
            }
        }
    }
}
