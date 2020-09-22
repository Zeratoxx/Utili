using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;

namespace Utili
{
    internal class VcRoles
    {
        public async Task Client_UserVoiceStateUpdated(SocketGuildUser user, SocketVoiceState before, SocketVoiceState after)
        {
            if (before.VoiceChannel == after.VoiceChannel) return;

            #region Remove Before Role

            if (before.VoiceChannel != null)
            {
                string id = "";
                try { id = GetFirstData(user.Guild.Id.ToString(), $"VCRoles-Role-{before.VoiceChannel.Id}").Value; }
                catch { }

                try
                {
                    SocketRole role = user.Guild.GetRole(ulong.Parse(id));
                    await user.RemoveRoleAsync(role);
                }
                catch { }
            }

            #endregion Remove Before Role

            #region Add After Role

            if (after.VoiceChannel != null)
            {
                string id = "";
                try { id = GetFirstData(user.Guild.Id.ToString(), $"VCRoles-Role-{after.VoiceChannel.Id}").Value; }
                catch { }

                try
                {
                    SocketRole role = user.Guild.GetRole(ulong.Parse(id));
                    await user.AddRoleAsync(role);
                }
                catch { }
            }

            #endregion Add After Role
        }
    }

    [Group("VCRoles")]
    public class VcRoleCommands : ModuleBase<SocketCommandContext>
    {
        public static string HelpContent =
                "help - Show this list\n" +
                "about - Display feature information\n" +
                "link [role] [voice channel]\n" +
                "unlink [voice channel]";

        [Command("Help")]
        public async Task Help()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("VC Roles", HelpContent, $"Prefix these commands with {prefix}vcroles"));
        }

        [Command("")]
        public async Task Empty()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("VC Roles", HelpContent, $"Prefix these commands with {prefix}vcroles"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("VC Roles", "You can link a role to a voice channel. While a user is in the voice channel you specified, they will have the role."));
        }

        [Command("Link"), Alias("Add")]
        public async Task Link(SocketRole role, [Remainder] SocketVoiceChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), $"VCRoles-Role-{channel.Id}");
                SaveData(Context.Guild.Id.ToString(), $"VCRoles-Role-{channel.Id}", role.Id.ToString());

                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Role linked", $"While users are in the voice channel {channel.Name}, they will be given the role {role.Mention}."));
            }
        }

        [Command("Unlink"), Alias("Remove")]
        public async Task Unlink([Remainder] SocketVoiceChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), $"VCRoles-Role-{channel.Id}");

                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Role unlinked", $"No roles will be modified when users join {channel.Name}"));
            }
        }
    }
}