using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using static Utili.Data;
using static Utili.Filter;
using static Utili.Logic;
using static Utili.Program;
using static Utili.SendMessage;

namespace Utili
{
    internal class Filter
    {
        public async Task Filter_MessageReceived(SocketMessage messageParam)
        {
            SocketUserMessage message = messageParam as SocketUserMessage;
            SocketCommandContext context = new SocketCommandContext(Client, message);

            if (context.User.Id == Client.CurrentUser.Id & context.Message.Embeds.Count > 0)
            {
                if (context.Message.Embeds.First().Author.Value.Name == "Message deleted") return;
            }

            if (DataExists(context.Guild.Id.ToString(), "Filter-Images", context.Channel.Id.ToString()))
            {
                bool delete = false;
                if (message.Attachments.Count == 0) delete = true;
                else
                {
                    string[] validFiles = { "png", "jpg" };
                    foreach (Attachment attachment in message.Attachments) if (!validFiles.Contains(attachment.Filename.Split(".").Last().ToLower())) delete = true;
                }

                if (delete)
                {
                    foreach (string word in message.Content.Split(" "))
                    {
                        if (await CheckImageAsync(word)) delete = false;
                    }
                }

                if (delete)
                {
                    await context.Message.DeleteAsync();
                    if (context.User.Id != Client.CurrentUser.Id)
                    {
                        RestUserMessage sentMessage = await context.Channel.SendMessageAsync(embed: GetEmbed("No", "Message deleted", "This channel only allows messages with image files (jpg, png) and instagram images."));
                        Thread.Sleep(5000);
                        await sentMessage.DeleteAsync();
                    }
                }
            }

            if (DataExists(context.Guild.Id.ToString(), "Filter-Videos", context.Channel.Id.ToString()))
            {
                bool delete = false;

                if (message.Attachments.Count > 0)
                {
                    string[] validFiles = { "mp4", "mov" };
                    foreach (Attachment attachment in message.Attachments) if (!validFiles.Contains(attachment.Filename.Split(".").Last().ToLower())) delete = true;
                }
                else delete = true;

                if (delete)
                {
                    foreach (string word in message.Content.Split(" "))
                    {
                        if (await CheckVideoAsync(word)) delete = false;
                    }
                }

                if (delete)
                {
                    await context.Message.DeleteAsync();
                    if (context.User.Id != Client.CurrentUser.Id)
                    {
                        RestUserMessage sentMessage = await context.Channel.SendMessageAsync(embed: GetEmbed("No", "Message deleted", "This channel only allows messages with video files (mov, mp4) or youtube links"));
                        Thread.Sleep(5000);
                        await sentMessage.DeleteAsync();
                    }
                }
            }

            if (DataExists(context.Guild.Id.ToString(), "Filter-Media", context.Channel.Id.ToString()))
            {
                bool delete = false;

                if (message.Attachments.Count > 0)
                {
                    string[] validFiles = { "png", "jpg", "mp4", "mov", "gif" };
                    foreach (Attachment attachment in message.Attachments) if (!validFiles.Contains(attachment.Filename.Split(".").Last().ToLower())) delete = true;
                }
                else delete = true;

                if (delete)
                {
                    foreach (string word in message.Content.Split(" "))
                    {
                        if (await CheckVideoAsync(word)) delete = false;
                        if (await CheckImageAsync(word)) delete = false;
                    }
                }

                if (delete)
                {
                    await context.Message.DeleteAsync();
                    if (context.User.Id != Client.CurrentUser.Id)
                    {
                        RestUserMessage sentMessage = await context.Channel.SendMessageAsync(embed: GetEmbed("No", "Message deleted", "This channel only allows messages with media files (png, jpg, mp4, mov, gif), youtube links and instagram images."));
                        Thread.Sleep(5000);
                        await sentMessage.DeleteAsync();
                    }
                }
            }

            if (DataExists(context.Guild.Id.ToString(), "Filter-Music", context.Channel.Id.ToString()))
            {
                bool delete = false;

                if (message.Attachments.Count > 0)
                {
                    string[] validFiles = { "mp3", "wav", "m4a", "flac" };
                    foreach (Attachment attachment in message.Attachments) if (!validFiles.Contains(attachment.Filename.Split(".").Last().ToLower())) delete = true;
                }
                else delete = true;

                if (delete)
                {
                    foreach (string word in message.Content.Split(" "))
                    {
                        if (await CheckVideoAsync(word)) delete = false;

                        if (word.ToLower().Contains("spotify.com/")) delete = false;
                        if (word.ToLower().Contains("soundcloud.com/")) delete = false;
                    }
                }

                if (delete)
                {
                    await context.Message.DeleteAsync();
                    if (context.User.Id != Client.CurrentUser.Id)
                    {
                        RestUserMessage sentMessage = await context.Channel.SendMessageAsync(embed: GetEmbed("No", "Message deleted", "This channel only allows messages with music files (mp3, wav, m4a, flac), youtube links, spotify links or soundcloud links"));
                        Thread.Sleep(5000);
                        await sentMessage.DeleteAsync();
                    }
                }
            }

            if (DataExists(context.Guild.Id.ToString(), "Filter-Attachments", context.Channel.Id.ToString()))
            {
                bool delete = false;

                if (message.Attachments.Count == 0) delete = true;

                if (delete)
                {
                    await context.Message.DeleteAsync();
                    if (context.User.Id != Client.CurrentUser.Id)
                    {
                        RestUserMessage sentMessage = await context.Channel.SendMessageAsync(embed: GetEmbed("No", "Message deleted", "This channel only allows messages with attachments"));
                        Thread.Sleep(5000);
                        await sentMessage.DeleteAsync();
                    }
                }
            }
        }

        public static void RemoveFilters(ulong guildId, ulong channelId)
        {
            DeleteData(guildId.ToString(), "Filter-Images", channelId.ToString());
            DeleteData(guildId.ToString(), "Filter-Videos", channelId.ToString());
            DeleteData(guildId.ToString(), "Filter-Media", channelId.ToString());
            DeleteData(guildId.ToString(), "Filter-Music", channelId.ToString());
            DeleteData(guildId.ToString(), "Filter-Attachments", channelId.ToString());
        }

        public static async Task<bool> CheckVideoAsync(string input)
        {
            Regex youtubeRegex = new Regex(@"^(http(s)??\:\/\/)?(www\.)?((youtube\.com\/watch\?v=)|(youtu.be\/))([a-zA-Z0-9\-_])+$");
            if (youtubeRegex.IsMatch(input))
            {
                string current = "";
                bool finished = false;
                foreach (char character in input.ToCharArray().Reverse())
                {
                    if (!finished)
                    {
                        switch (character.ToString())
                        {
                            case "?":
                                current = "";
                                break;

                            case "#":
                                current = "";
                                break;

                            case "&":
                                current = "";
                                break;

                            case "=":
                                finished = true;
                                break;

                            case "/":
                                finished = true;
                                break;

                            default:
                                current += character.ToString();
                                break;
                        }
                    }
                }

                string id = "";
                foreach (char character in current.Reverse()) id += character;
                try
                {
                    VideosResource.ListRequest request = Youtube.Videos.List("snippet");
                    request.Id = id;
                    VideoListResponse response = await request.ExecuteAsync();
                    if (response.Items.Count > 0) return true;
                }
                catch { }
            }
            return false;
        }

        public static async Task<bool> CheckImageAsync(string input)
        {
            if (!input.ToLower().Contains("instagram.com/p/")) return false;

            try
            {
                WebClient webClient = new WebClient();
                string content = webClient.DownloadString(input);

                if (content.Contains("Sorry, this page isn't available.") && content.Contains("The link you followed may be broken, or the page may have been removed.")) return false;

                return true;
            }
            catch { return false; }
        }
    }

    [Group("Filter")]
    public class FilterCommands : ModuleBase<SocketCommandContext>
    {
        public static string HelpContent =
                "help - Show this list\n" +
                "about - Display feature information\n" +
                "images [channel] - Allow only image files and instagram images in a channel\n" +
                "videos [channel] - Allow only video files and youtube links in a channel\n" +
                "media [channel] - Allow only image files, video files, gif files, youtube links and instagram images in a channel\n" +
                "music [channel] - Allow only music files, spotify links, youtube links and soundcloud links in a channel\n" +
                "attachments [channel] - Allow only messages with attachments in a channel\n" +
                "off [channel] - Disable any filters in a channel";

        [Command("Help")]
        public async Task Help()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Message Filter", HelpContent, $"Prefix these commands with {prefix}filter"));
        }

        [Command("")]
        public async Task Empty()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Message Filter", HelpContent, $"Prefix these commands with {prefix}filter"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Message Filter", "This feature can be configured to only allow certain types of message in certain channels."));
        }

        [Command("Images")]
        public async Task Images(ITextChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(channel, new[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.ManageMessages }, Context.Channel))
                {
                    RemoveFilters(Context.Guild.Id, channel.Id);
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Filter enabled", $"Only allowing messages with images in {channel.Mention}"));
                    await Task.Delay(500);
                    SaveData(Context.Guild.Id.ToString(), "Filter-Images", channel.Id.ToString());
                }
            }
        }

        [Command("Videos")]
        public async Task Videos(ITextChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(channel, new[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.ManageMessages }, Context.Channel))
                {
                    RemoveFilters(Context.Guild.Id, channel.Id);
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Filter enabled", $"Only allowing messages with videos in {channel.Mention}"));
                    await Task.Delay(500);
                    SaveData(Context.Guild.Id.ToString(), "Filter-Videos", channel.Id.ToString());
                }
            }
        }

        [Command("Media")]
        public async Task Media(ITextChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(channel, new[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.ManageMessages }, Context.Channel))
                {
                    RemoveFilters(Context.Guild.Id, channel.Id);
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Filter enabled", $"Only allowing messages with images or videos in {channel.Mention}"));
                    await Task.Delay(500);
                    SaveData(Context.Guild.Id.ToString(), "Filter-Media", channel.Id.ToString());
                }
            }
        }

        [Command("Music")]
        public async Task Music(ITextChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(channel, new[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.ManageMessages }, Context.Channel))
                {
                    RemoveFilters(Context.Guild.Id, channel.Id);
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Filter enabled", $"Only allowing messages with music in {channel.Mention}"));
                    await Task.Delay(500);
                    SaveData(Context.Guild.Id.ToString(), "Filter-Music", channel.Id.ToString());
                }
            }
        }

        [Command("Attachments")]
        public async Task Attachments(ITextChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(channel, new[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.ManageMessages }, Context.Channel))
                {
                    RemoveFilters(Context.Guild.Id, channel.Id);
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Filter enabled", $"Only allowing messages with attachments in {channel.Mention}"));
                    await Task.Delay(500);
                    SaveData(Context.Guild.Id.ToString(), "Filter-Attachments", channel.Id.ToString());
                }
            }
        }

        [Command("Off")]
        public async Task Off(ITextChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                RemoveFilters(Context.Guild.Id, channel.Id);
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Filters disabled", $"Allowing all messages in {channel.Mention}"));
            }
        }
    }
}