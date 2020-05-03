using System;
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
using static Utili.Json;
using Discord.Rest;

namespace Utili
{
    class VCLink
    {
        public async Task Client_UserVoiceStateUpdated(SocketGuildUser User, SocketVoiceState Before, SocketVoiceState After)
        {
            if (After.VoiceChannel != null)
            {
                DeleteData(User.Guild.Id.ToString(), $"InactiveRole-Timer-{User.Id}");
                SaveData(User.Guild.Id.ToString(), $"InactiveRole-Timer-{User.Id}", ToSQLTime(DateTime.Now));
            }

            bool VCLinkEnabled = GetData(User.Guild.Id.ToString(), "VCLink-Enabled", "True").Count > 0;
            if (VCLinkEnabled || GetData(User.Guild.Id.ToString(), $"VCLink-Channel-{Before.VoiceChannel.Id}").Count > 0)
            {
                #region Remove Before VC

                if(Before.VoiceChannel != null)
                {
                    try
                    {
                        SocketTextChannel Channel = User.Guild.GetTextChannel(ulong.Parse(GetData(User.Guild.Id.ToString(), $"VCLink-Channel-{Before.VoiceChannel.Id}").First().Value));
                        await Channel.RemovePermissionOverwriteAsync(User);
                        if (User.Guild.Users.Where(x => x.VoiceChannel != null).Where(x => x.VoiceChannel.Id == Before.VoiceChannel.Id).Count() == 0)
                        {
                            await Channel.DeleteAsync();
                            DeleteData(User.Guild.Id.ToString(), $"VCLink-Channel-{Before.VoiceChannel.Id}");
                        }
                    }
                    catch { };
                }

                #endregion

                if (VCLinkEnabled)
                {
                    #region Add After VC
                    ulong AFKID = 0;
                    try { AFKID = User.Guild.AFKChannel.Id; } catch { }
                    if (After.VoiceChannel != null && After.VoiceChannel.Id != AFKID)
                    {
                        DeleteData(User.Guild.Id.ToString(), $"InactiveRole-Timer-{User.Id}");
                        SaveData(User.Guild.Id.ToString(), $"InactiveRole-Timer-{User.Id}", ToSQLTime(DateTime.Now));

                        SocketTextChannel Channel;
                        try { Channel = User.Guild.GetTextChannel(ulong.Parse(GetData(User.Guild.Id.ToString(), $"VCLink-Channel-{After.VoiceChannel.Id}").First().Value)); }
                        catch
                        {
                            var Temp = await User.Guild.CreateTextChannelAsync($"VC-{After.VoiceChannel.Name}");
                            await Task.Delay(100);
                            Channel = User.Guild.GetTextChannel(Temp.Id);
                            SaveData(User.Guild.Id.ToString(), $"VCLink-Channel-{After.VoiceChannel.Id}", Channel.Id.ToString());

                            if (After.VoiceChannel.CategoryId.HasValue) await Channel.ModifyAsync(x => x.CategoryId = After.VoiceChannel.CategoryId.Value);
                            await Channel.ModifyAsync(x => x.Topic = $"Automatically made by Utili");
                            await Channel.AddPermissionOverwriteAsync(User.Guild.EveryoneRole, new OverwritePermissions(viewChannel: PermValue.Deny));
                        }

                        await Channel.AddPermissionOverwriteAsync(User, new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow));
                    }

                    #endregion
                }
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
                "off - Disable the feature in the server";

        [Command("Help")]
        public async Task Help()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("VC Linking", HelpContent, $"Prefix these commands with {Prefix}vclink"));
        }

        [Command("")]
        public async Task Empty()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
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
    }
}
