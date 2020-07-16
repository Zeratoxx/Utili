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

using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;
using static Utili.Json;

namespace Utili
{
    class RolePersist
    {
        public async Task UserJoin(SocketGuildUser User)
        {
            if (GetData(User.Guild.Id.ToString(), "RolePersist-Enabled", "True").Count > 0)
            {
                foreach (Data Data in GetData(User.Guild.Id.ToString(), $"RolePersist-Role-{User.Id}", IgnoreCache: true))
                {
                    try
                    {
                        var Role = User.Guild.Roles.First(x => x.Id.ToString() == Data.Value);
                        _ = User.AddRoleAsync(Role);
                    }
                    catch { };
                }
                DeleteData(User.Guild.Id.ToString(), $"RolePersist-Role-{User.Id}");
            }
        }

        public async Task UserLeft(SocketGuildUser User)
        {
            if(GetData(User.Guild.Id.ToString(), "RolePersist-Enabled", "True").Count > 0)
            {
                foreach(var Role in User.Roles)
                {
                    SaveData(User.Guild.Id.ToString(), $"RolePersist-Role-{User.Id}", Role.Id.ToString(), IgnoreCache: true);
                }
            }
        }
    }

    [Group("RolePersist")]
    public class RolePersistCommands : ModuleBase<SocketCommandContext>
    {
        public static string HelpContent =
                "help - Show this list\n" +
                "about - Display feature information\n" +
                "on - Enable role persist\n" +
                "off - Disable role persist";

        [Command("Help")]
        public async Task Help()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Role Persist", HelpContent, $"Prefix these commands with {Prefix}rolePersist"));
        }

        [Command("")]
        public async Task Empty()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Role Persist", HelpContent, $"Prefix these commands with {Prefix}rolePersist"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Role Persist", "If this feature is enabled, users that re-join the server will be given all of the roles that they had when they left."));
        }

        [Command("On"), Alias("Enable")]
        public async Task On()
        {
            if(Permission(Context.User, Context.Channel))
            {
                if(BotHasPermissions(Context.Guild, new GuildPermission[] { GuildPermission.ManageRoles }, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "RolePersist-Enabled");
                    SaveData(Context.Guild.Id.ToString(), "RolePersist-Enabled", "True");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Role persist enabled", "Starting from now, users that leave the server will be given back their roles when they re-join\nMake sure my top role is higher than the roles you give to users!"));
                }
            }
        }

        [Command("Off"), Alias("Disable")]
        public async Task Off()
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "RolePersist-Enabled");
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Role persist disabled"));
            }
        }
    }
}
