using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;

namespace Utili
{
    internal class VCRoles
    {
        public async Task Client_UserVoiceStateUpdated(SocketGuildUser User, SocketVoiceState Before, SocketVoiceState After)
        {
            if (Before.VoiceChannel == After.VoiceChannel) return;

            #region Remove Before Role

            if (Before.VoiceChannel != null)
            {
                string ID = "";
                try { ID = GetFirstData(User.Guild.Id.ToString(), $"VCRoles-Role-{Before.VoiceChannel.Id}").Value; }
                catch { }

                try
                {
                    SocketRole Role = User.Guild.GetRole(ulong.Parse(ID));
                    await User.RemoveRoleAsync(Role);
                }
                catch { };
            }

            #endregion Remove Before Role

            #region Add After Role

            if (Before.VoiceChannel != null)
            {
                string ID = "";
                try { ID = GetFirstData(User.Guild.Id.ToString(), $"VCRoles-Role-{Before.VoiceChannel.Id}").Value; }
                catch { }

                try
                {
                    SocketRole Role = User.Guild.GetRole(ulong.Parse(ID));
                    await User.AddRoleAsync(Role);
                }
                catch { };
            }

            #endregion Add After Role
        }
    }

    [Group("VCRoles")]
    public class VCRoleCommands : ModuleBase<SocketCommandContext>
    {
        public static string HelpContent =
                "help - Show this list\n" +
                "about - Display feature information\n" +
                "link [role] [voice channel]\n" +
                "unlink [voice channel]";

        [Command("Help")]
        public async Task Help()
        {
            string Prefix = ".";
            try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("VC Roles", HelpContent, $"Prefix these commands with {Prefix}vcroles"));
        }

        [Command("")]
        public async Task Empty()
        {
            string Prefix = ".";
            try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("VC Roles", HelpContent, $"Prefix these commands with {Prefix}vcroles"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("VC Roles", "You can link a role to a voice channel. While a user is in the voice channel you specified, they will have the role."));
        }

        [Command("Link"), Alias("Add")]
        public async Task Link(SocketRole Role, [Remainder] SocketVoiceChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), $"VCRoles-Role-{Channel.Id}");
                SaveData(Context.Guild.Id.ToString(), $"VCRoles-Role-{Channel.Id}", Role.Id.ToString());

                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Role linked", $"While users are in the voice channel {Channel.Name}, they will be given the role {Role.Mention}."));
            }
        }

        [Command("Unlink"), Alias("Remove")]
        public async Task Unlink([Remainder] SocketVoiceChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), $"VCRoles-Role-{Channel.Id}");

                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Role unlinked", $"No roles will be modified when users join {Channel.Name}"));
            }
        }
    }
}