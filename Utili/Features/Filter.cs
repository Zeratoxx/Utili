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
using static Utili.Filter;
using static Utili.Json;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Utili
{
    class Filter
    {
        public async Task Filter_MessageReceived(SocketMessage MessageParam)
        {
            var Message = MessageParam as SocketUserMessage;
            var Context = new SocketCommandContext(Client, Message);

            if (Context.User.Id == Program.Client.CurrentUser.Id & Context.Message.Embeds.Count > 0)
            {
                if (Context.Message.Embeds.First().Author.Value.Name == "Message deleted") return;
            }

            if(GetDataWhere($"GuildID = '{Context.Guild.Id}' AND DataType LIKE '%Filter-%' AND DataValue = '{Context.Channel.Id}'").Count > 0)
            {
                if (GetData(Context.Guild.Id.ToString(), "Filter-Images", Context.Channel.Id.ToString()).Count > 0)
                {
                    bool Delete = false;
                    if (Message.Attachments.Count == 0) Delete = true;
                    else
                    {
                        string[] ValidFiles = { "png", "jpg" };
                        foreach (Attachment Attachment in Message.Attachments) if (!ValidFiles.Contains(Attachment.Filename.Split(".").Last().ToLower())) Delete = true;
                    }

                    if (Delete)
                    {
                        await Context.Message.DeleteAsync();
                        if (Context.User.Id != Program.Client.CurrentUser.Id)
                        {
                            var SentMessage = await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Message deleted", "This channel only allows messages with image files (jpg, png)"));
                            Thread.Sleep(5000);
                            await SentMessage.DeleteAsync();
                        }
                    }
                }

                if (GetData(Context.Guild.Id.ToString(), "Filter-Videos", Context.Channel.Id.ToString()).Count > 0)
                {
                    bool Delete = false;

                    if (Message.Attachments.Count > 0)
                    {
                        string[] ValidFiles = { "mp4", "mov" };
                        foreach (Attachment Attachment in Message.Attachments) if (!ValidFiles.Contains(Attachment.Filename.Split(".").Last().ToLower())) Delete = true;
                    }
                    else Delete = true;

                    if (Delete)
                    {
                        foreach (string Word in Message.Content.Split(" "))
                        {
                            if (await CheckVideoAsync(Word)) Delete = false;
                        }
                    }

                    if (Delete)
                    {
                        await Context.Message.DeleteAsync();
                        if (Context.User.Id != Program.Client.CurrentUser.Id)
                        {
                            var SentMessage = await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Message deleted", "This channel only allows messages with video files (mov, mp4) or youtube links"));
                            Thread.Sleep(5000);
                            await SentMessage.DeleteAsync();
                        }
                    }
                }

                if (GetData(Context.Guild.Id.ToString(), "Filter-Media", Context.Channel.Id.ToString()).Count > 0)
                {
                    bool Delete = false;

                    if (Message.Attachments.Count > 0)
                    {
                        string[] ValidFiles = { "png", "jpg", "mp4", "mov", "gif" };
                        foreach (Attachment Attachment in Message.Attachments) if (!ValidFiles.Contains(Attachment.Filename.Split(".").Last().ToLower())) Delete = true;
                    }
                    else Delete = true;

                    if (Delete)
                    {
                        foreach (string Word in Message.Content.Split(" "))
                        {
                            if (await CheckVideoAsync(Word)) Delete = false;
                        }
                    }

                    if (Delete)
                    {
                        await Context.Message.DeleteAsync();
                        if (Context.User.Id != Program.Client.CurrentUser.Id)
                        {
                            var SentMessage = await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Message deleted", "This channel only allows messages with media files (png, jpg, mp4, mov, gif) or youtube links"));
                            Thread.Sleep(5000);
                            await SentMessage.DeleteAsync();
                        }
                    }
                }

                if (GetData(Context.Guild.Id.ToString(), "Filter-Music", Context.Channel.Id.ToString()).Count > 0)
                {
                    bool Delete = false;

                    if (Message.Attachments.Count > 0)
                    {
                        string[] ValidFiles = { "mp3", "wav", "m4a", "flac" };
                        foreach (Attachment Attachment in Message.Attachments) if (!ValidFiles.Contains(Attachment.Filename.Split(".").Last().ToLower())) Delete = true;
                    }
                    else Delete = true;

                    if (Delete)
                    {
                        foreach (string Word in Message.Content.Split(" "))
                        {
                            if (await CheckVideoAsync(Word)) Delete = false;

                            if (Word.ToLower().Contains("spotify.com/")) Delete = false;
                            if (Word.ToLower().Contains("soundcloud.com/")) Delete = false;
                        }
                    }

                    if (Delete)
                    {
                        await Context.Message.DeleteAsync();
                        if (Context.User.Id != Program.Client.CurrentUser.Id)
                        {
                            var SentMessage = await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Message deleted", "This channel only allows messages with music files (mp3, wav, m4a, flac), youtube links, spotify links or soundcloud links"));
                            Thread.Sleep(5000);
                            await SentMessage.DeleteAsync();
                        }
                    }
                }

                if (GetData(Context.Guild.Id.ToString(), "Filter-Attachments", Context.Channel.Id.ToString()).Count > 0)
                {
                    bool Delete = false;

                    if (Message.Attachments.Count == 0) Delete = true;

                    if (Delete)
                    {
                        await Context.Message.DeleteAsync();
                        if (Context.User.Id != Program.Client.CurrentUser.Id)
                        {
                            var SentMessage = await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Message deleted", "This channel only allows messages with attachments"));
                            Thread.Sleep(5000);
                            await SentMessage.DeleteAsync();
                        }
                    }
                }
            }
        }

        public static void RemoveFilters(ulong GuildID, ulong ChannelID)
        {
            DeleteData(GuildID.ToString(), "Filter-Images", ChannelID.ToString());
            DeleteData(GuildID.ToString(), "Filter-Videos", ChannelID.ToString());
            DeleteData(GuildID.ToString(), "Filter-Media", ChannelID.ToString());
            DeleteData(GuildID.ToString(), "Filter-Music", ChannelID.ToString());
            DeleteData(GuildID.ToString(), "Filter-Attachments", ChannelID.ToString());
        }

        public static async Task<bool> CheckVideoAsync(string Input)
        {
            Regex YoutubeRegex = new Regex(@"^(http(s)??\:\/\/)?(www\.)?((youtube\.com\/watch\?v=)|(youtu.be\/))([a-zA-Z0-9\-_])+$");
            if (YoutubeRegex.IsMatch(Input))
            {
                string Current = "";
                bool Finished = false;
                foreach (var Character in Input.ToCharArray().Reverse())
                {
                    if (!Finished)
                    {
                        switch (Character.ToString())
                        {
                            case "?":
                                Current = "";
                                break;
                            case "#":
                                Current = "";
                                break;
                            case "&":
                                Current = "";
                                break;
                            case "=":
                                Finished = true;
                                break;
                            case "/":
                                Finished = true;
                                break;
                            default:
                                Current += Character.ToString();
                                break;
                        }
                    }
                }

                string ID = "";
                foreach (var Character in Current.Reverse()) ID += Character;
                try
                {
                    var Request = Youtube.Videos.List("snippet");
                    Request.Id = ID;
                    var Response = await Request.ExecuteAsync();
                    if (Response.Items.Count > 0) return true;
                }
                catch (Exception e) { }
            }
            return false;
        }
    }

    [Group("Filter")]
    public class FilterCommands : ModuleBase<SocketCommandContext>
    {
        public static string HelpContent =
                "help - Show this list\n" +
                "about - Display feature information\n" +
                "images [channel] - Allow only image files in a channel\n" +
                "videos [channel] - Allow only video files and youtube links in a channel\n" +
                "media [channel] - Allow only image files, video files, gif files and youtube links in a channel\n" +
                "music [channel] - Allow only music files, spotify links, youtube links and soundcloud links in a channel\n" +
                "attachments [channel] - Allow only messages with attachments in a channel\n" +
                "off [channel] - Disable any filters in a channel";

        [Command("Help")]
        public async Task Help()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Message Filter", HelpContent, $"Prefix these commands with {Prefix}filter"));
        }

        [Command("")]
        public async Task Empty()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Message Filter", HelpContent, $"Prefix these commands with {Prefix}filter"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Message Filter", "This feature can be configured to only allow certain types of message in certain channels."));
        }

        [Command("Images")]
        public async Task Images(ITextChannel Channel)
        {
            if(Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(Channel, new ChannelPermission[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.ManageMessages }, Context.Channel))
                {
                    RemoveFilters(Context.Guild.Id, Channel.Id);
                    SaveData(Context.Guild.Id.ToString(), "Filter-Images", Channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Filter enabled", $"Only allowing messages with images in {Channel.Mention}"));
                }  
            }
        }

        [Command("Videos")]
        public async Task Videos(ITextChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(Channel, new ChannelPermission[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.ManageMessages }, Context.Channel))
                {
                    RemoveFilters(Context.Guild.Id, Channel.Id);
                    SaveData(Context.Guild.Id.ToString(), "Filter-Videos", Channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Filter enabled", $"Only allowing messages with videos in {Channel.Mention}"));
                }
            }
        }

        [Command("Media")]
        public async Task Media(ITextChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(Channel, new ChannelPermission[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.ManageMessages }, Context.Channel))
                {
                    RemoveFilters(Context.Guild.Id, Channel.Id);
                    SaveData(Context.Guild.Id.ToString(), "Filter-Media", Channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Filter enabled", $"Only allowing messages with images or videos in {Channel.Mention}"));
                }
            }
        }

        [Command("Music")]
        public async Task Music(ITextChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(Channel, new ChannelPermission[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.ManageMessages }, Context.Channel))
                {
                    RemoveFilters(Context.Guild.Id, Channel.Id);
                    SaveData(Context.Guild.Id.ToString(), "Filter-Music", Channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Filter enabled", $"Only allowing messages with music in {Channel.Mention}"));
                }
            }
        }

        [Command("Attachments")]
        public async Task Attachments(ITextChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(Channel, new ChannelPermission[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.ManageMessages }, Context.Channel))
                {
                    RemoveFilters(Context.Guild.Id, Channel.Id);
                    SaveData(Context.Guild.Id.ToString(), "Filter-Attachments", Channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Filter enabled", $"Only allowing messages with attachments in {Channel.Mention}"));
                }
            }
        }

        [Command("Off")]
        public async Task Off(ITextChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                RemoveFilters(Context.Guild.Id, Channel.Id);
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Filters disabled", $"Allowing all messages in {Channel.Mention}"));
            }
        }
    }
}
