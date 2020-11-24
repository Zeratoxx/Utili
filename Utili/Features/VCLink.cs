using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;

namespace Utili
{
    internal class VcLink
    {
        public async Task Client_UserVoiceStateUpdated(SocketGuildUser user, SocketVoiceState before, SocketVoiceState after)
        {
            if (before.VoiceChannel == after.VoiceChannel) return;

            // VCLinkEnabled effects only the after voice channel. The before voice channel is updated regardless of settings.
            bool vcLinkEnabled = DataExists(user.Guild.Id.ToString(), "VCLink-Enabled", "True");
            if (vcLinkEnabled)
            {
                try
                {
                    if (DataExists(user.Guild.Id.ToString(), "VCLink-Exclude", after.VoiceChannel.Id.ToString())) vcLinkEnabled = false;
                }
                catch { }
            }

            #region Remove Before VC

            if (before.VoiceChannel != null)
            {
                string id = "";
                try { id = GetFirstData(user.Guild.Id.ToString(), $"VCLink-Channel-{before.VoiceChannel.Id}").Value; }
                catch { }
                try
                {
                    SocketTextChannel channel = user.Guild.GetTextChannel(ulong.Parse(id));
                    await channel.RemovePermissionOverwriteAsync(user);
                    if (user.Guild.Users.Where(x => x.VoiceChannel != null && !x.IsBot).Where(x => x.VoiceChannel.Id == before.VoiceChannel.Id).Count() == 0)
                    {
                        if(GetPerms(channel).ManageChannel) await channel.DeleteAsync();
                        DeleteData(user.Guild.Id.ToString(), $"VCLink-Channel-{before.VoiceChannel.Id}");
                    }
                }
                catch { }
            }

            #endregion Remove Before VC

            if (vcLinkEnabled)
            {
                #region Add After VC

                ulong afkid = 0;
                try { afkid = user.Guild.AFKChannel.Id; } catch { }

                if (after.VoiceChannel != null && after.VoiceChannel.Id != afkid)
                {
                    string id = "";
                    try { id = GetFirstData(user.Guild.Id.ToString(), $"VCLink-Channel-{after.VoiceChannel.Id}").Value; }
                    catch { }

                    SocketTextChannel channel = null;
                    try { channel = user.Guild.GetTextChannel(ulong.Parse(id)); }
                    catch { }
                    
                    if(channel == null)
                    {
                        DeleteData(user.Guild.Id.ToString(), $"VCLink-Channel-{after.VoiceChannel.Id}");

                        if(!GetPerms(user.Guild).ManageChannels) return;
                        RestTextChannel temp = await user.Guild.CreateTextChannelAsync($"vc-{after.VoiceChannel.Name}");
                        SaveData(user.Guild.Id.ToString(), $"VCLink-Channel-{after.VoiceChannel.Id}", temp.Id.ToString());

                        if (after.VoiceChannel.CategoryId.HasValue) await temp.ModifyAsync(x => { x.CategoryId = after.VoiceChannel.CategoryId.Value; x.Topic = "Automatically made by Utili"; });
                        await channel.AddPermissionOverwriteAsync(user.Guild.EveryoneRole, new OverwritePermissions(viewChannel: PermValue.Deny));
                    }

                    await channel.AddPermissionOverwriteAsync(user, new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow));
                }

                #endregion Add After VC
            }
        }
    }

    [Group("VCLink")]
    public class VcLinkCommands : ModuleBase<SocketCommandContext>
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
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("VC Linking", HelpContent, $"Prefix these commands with {prefix}vclink"));
        }

        [Command("")]
        public async Task Empty()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("VC Linking", HelpContent, $"Prefix these commands with {prefix}vclink"));
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
                if (BotHasPermissions(Context.Guild, new[] { GuildPermission.ManageChannels }, Context.Channel))
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
        public async Task Exclude([Remainder] IVoiceChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "VCLink-Exclude", channel.Id.ToString());
                SaveData(Context.Guild.Id.ToString(), "VCLink-Exclude", channel.Id.ToString());

                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Voice channel excluded", $"Text channels will no longer be made for the voice channel {channel.Name}"));
            }
        }

        [Command("Include"), Alias("Unexclude")]
        public async Task Include([Remainder] IVoiceChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "VCLink-Exclude", channel.Id.ToString());

                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Voice channel included", $"Text channels will no longer no longer be made for the voice channel {channel.Name}\n**Note:** This command doesn't enable the feature, it only reverses the effect of vclink exclude. Use vclink on to enable the feature in your server."));
            }
        }
    }
}