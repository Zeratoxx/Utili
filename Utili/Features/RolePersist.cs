using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;

namespace Utili
{
    internal class RolePersist
    {
        public async Task UserJoin(SocketGuildUser user)
        {
            if(!GetPerms(user.Guild).ManageRoles) return;

            if (DataExists(user.Guild.Id.ToString(), "RolePersist-Enabled", "True"))
            {
                foreach (Data data in GetData(user.Guild.Id.ToString(), $"RolePersist-Role-{user.Id}", ignoreCache: true))
                {
                    try
                    {
                        SocketRole role = user.Guild.Roles.First(x => x.Id.ToString() == data.Value);
                        _ = user.AddRoleAsync(role);
                    }
                    catch { }
                }
                DeleteData(user.Guild.Id.ToString(), $"RolePersist-Role-{user.Id}");
            }
        }

        public async Task UserLeft(SocketGuildUser user)
        {
            if (DataExists(user.Guild.Id.ToString(), "RolePersist-Enabled", "True"))
            {
                foreach (SocketRole role in user.Roles)
                {
                    SaveData(user.Guild.Id.ToString(), $"RolePersist-Role-{user.Id}", role.Id.ToString(), ignoreCache: true);
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
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Role Persist", HelpContent, $"Prefix these commands with {prefix}rolePersist"));
        }

        [Command("")]
        public async Task Empty()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Role Persist", HelpContent, $"Prefix these commands with {prefix}rolePersist"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Role Persist", "If this feature is enabled, users that re-join the server will be given all of the roles that they had when they left."));
        }

        [Command("On"), Alias("Enable")]
        public async Task On()
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(Context.Guild, new[] { GuildPermission.ManageRoles }, Context.Channel))
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