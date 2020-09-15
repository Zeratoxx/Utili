using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;
using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;

namespace Utili
{
    internal class VCLink
    {
        public async Task Client_UserVoiceStateUpdated(SocketGuildUser User, SocketVoiceState Before, SocketVoiceState After)
        {
            if (Before.VoiceChannel == After.VoiceChannel) return;

            // VCLinkEnabled effects only the after voice channel. The before voice channel is updated regardless of settings.
            bool VCLinkEnabled = DataExists(User.Guild.Id.ToString(), "VCLink-Enabled", "True");
            if (VCLinkEnabled)
            {
                try
                {
                    if (DataExists(User.Guild.Id.ToString(), $"VCLink-Exclude", After.VoiceChannel.Id.ToString())) VCLinkEnabled = false;
                }
                catch { }
            }

            #region Remove Before VC

            if (Before.VoiceChannel != null)
            {
                string ID = "";
                try { ID = GetFirstData(User.Guild.Id.ToString(), $"VCLink-Channel-{Before.VoiceChannel.Id}").Value; }
                catch { }
                bool Success = false;
                try
                {
                    SocketTextChannel Channel = User.Guild.GetTextChannel(ulong.Parse(ID));
                    await Channel.RemovePermissionOverwriteAsync(User);
                    if (User.Guild.Users.Where(x => x.VoiceChannel != null && !x.IsBot).Where(x => x.VoiceChannel.Id == Before.VoiceChannel.Id).Count() == 0)
                    {
                        await Channel.DeleteAsync();
                        DeleteData(User.Guild.Id.ToString(), $"VCLink-Channel-{Before.VoiceChannel.Id}");
                    }

                    Success = true;
                }
                catch { };
            }

            #endregion Remove Before VC

            if (VCLinkEnabled)
            {
                #region Add After VC

                ulong AFKID = 0;
                try { AFKID = User.Guild.AFKChannel.Id; } catch { }

                if (After.VoiceChannel != null && After.VoiceChannel.Id != AFKID)
                {
                    string ID = "";
                    try { ID = GetFirstData(User.Guild.Id.ToString(), $"VCLink-Channel-{After.VoiceChannel.Id}").Value; }
                    catch { }

                    SocketTextChannel Channel = null;
                    try { Channel = User.Guild.GetTextChannel(ulong.Parse(ID)); }
                    catch { }
                    
                    if(Channel == null)
                    {
                        DeleteData(User.Guild.Id.ToString(), $"VCLink-Channel-{After.VoiceChannel.Id}");

                        var Temp = await User.Guild.CreateTextChannelAsync($"vc-{After.VoiceChannel.Name}");
                        SaveData(User.Guild.Id.ToString(), $"VCLink-Channel-{After.VoiceChannel.Id}", Temp.Id.ToString());

                        int i = 0;
                        while (Channel == null || i > 20)
                        {
                            await Task.Delay(500);
                            Channel = User.Guild.GetTextChannel(Temp.Id);
                            i++;
                        }

                        if (After.VoiceChannel.CategoryId.HasValue) await Channel.ModifyAsync(x => { x.CategoryId = After.VoiceChannel.CategoryId.Value; x.Topic = "Automatically made by Utili"; });
                        await Channel.AddPermissionOverwriteAsync(User.Guild.EveryoneRole, new OverwritePermissions(viewChannel: PermValue.Deny));
                    }

                    await Channel.AddPermissionOverwriteAsync(User, new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow));
                }

                #endregion Add After VC
            }
        }
    }

    [Group("VCLink")]
    public class VCLinkCommands : ModuleBase<SocketCommandContext>
    {
        public static string HelpContent =
                "help - Show this list\n" +
                "about - Display feature information\n" +
                "on - Enable the feature in the server\n" +
                "off - Disable the feature in the server\n" +
                "exclude [voice channel] - Exclude a voice channel from getting a text channel linked to it\n" +
                "include [voice channel] - Reverse the effect of the exclude command\n";

        [Command("Help")]
        public async Task Help()
        {
            string Prefix = ".";
            try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("VC Linking", HelpContent, $"Prefix these commands with {Prefix}vclink"));
        }

        [Command("")]
        public async Task Empty()
        {
            string Prefix = ".";
            try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("VC Linking", HelpContent, $"Prefix these commands with {Prefix}vclink"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("VC Linking", "When a user joins a voice channel they get access to a text channel which is created in the same category as the voice channel. Their access is removed once they leave the channel, and if all users have left the voice channel the text channel is deleted.\nIf the bot is offline when the users disconnect from the voice channel it will not be deleted or their permissions removed.\nThis feature does not create text channels for the AFK voice channel of your guild."));
        }

        [Command("On"), Alias("Enable")]
        public async Task On()
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(Context.Guild, new GuildPermission[] { GuildPermission.ManageChannels }, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "VCLink-Enabled");
                    SaveData(Context.Guild.Id.ToString(), "VCLink-Enabled", "True");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Voice channel linking enabled"));
                }
            }
        }

        [Command("Off"), Alias("Disable")]
        public async Task Off()
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "VCLink-Enabled");
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Voice channel linking disabled"));
            }
        }

        [Command("Exclude")]
        public async Task Exclude([Remainder] IVoiceChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "VCLink-Exclude", Channel.Id.ToString());
                SaveData(Context.Guild.Id.ToString(), "VCLink-Exclude", Channel.Id.ToString());

                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Voice channel excluded", $"Text channels will no longer be made for the voice channel {Channel.Name}"));
            }
        }

        [Command("Include"), Alias("Unexclude")]
        public async Task Include([Remainder] IVoiceChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "VCLink-Exclude", Channel.Id.ToString());

                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Voice channel included", $"Text channels will no longer no longer be made for the voice channel {Channel.Name}\n**Note:** This command doesn't enable the feature, it only reverses the effect of vclink exclude. Use vclink on to enable the feature in your server."));
            }
        }
    }
}