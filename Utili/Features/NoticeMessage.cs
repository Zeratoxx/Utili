using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using static Utili.Data;
using static Utili.Logic;
using static Utili.Program;
using static Utili.SendMessage;

namespace Utili
{
    internal class NoticeMessage
    {
        private static readonly List<(ulong, int)> MessageList = new List<(ulong, int)>();

        public async Task NoticeMessage_MessageReceived(SocketMessage messageParam)
        {
            SocketUserMessage message = messageParam as SocketUserMessage;
            SocketCommandContext context = new SocketCommandContext(Client, message);

            if (message.Author.Id == Client.CurrentUser.Id)
            {
                await Task.Delay(5000);
                IMessage newMsg = await context.Channel.GetMessageAsync(message.Id);
                if (newMsg.IsPinned) return;
            }

            int thisNumber;
            try { thisNumber = MessageList.OrderBy(x => x.Item2).Last(x => x.Item1 == context.Channel.Id).Item2 + 1; }
            catch { thisNumber = 0; }

            MessageList.RemoveAll(x => x.Item1 == context.Channel.Id);
            MessageList.Add((context.Channel.Id, thisNumber));

            if (DataExists($"{context.Guild.Id}", "Notices-Channel", $"{context.Channel.Id}"))
            {
                TimeSpan delay = TimeSpan.FromSeconds(15);
                try { delay = TimeSpan.Parse(GetFirstData($"{context.Guild.Id}", $"Notices-Delay-{context.Channel.Id}").Value); }
                catch { }

                await Task.Delay(int.Parse(Math.Ceiling(delay.TotalMilliseconds).ToString()));

                int newNumber;
                try { newNumber = MessageList.OrderBy(x => x.Item2).Last(x => x.Item1 == context.Channel.Id).Item2; }
                catch { newNumber = 0; }

                if (thisNumber == newNumber) await Update(context.Channel as ITextChannel, true, true);
                else return;
            }
        }

        public async Task Update(ITextChannel channel, bool save = true, bool knownOn = false)
        {
            if (!knownOn) knownOn = DataExists($"{channel.Guild.Id}", "Notices-Channel", $"{channel.Id}");
            if (knownOn || !save)
            {
                IMessage noticeMessage;
                try
                {
                    Data messageVar = GetFirstData($"{channel.Guild.Id}", $"Notices-Message-{channel.Id}");
                    noticeMessage = await channel.GetMessageAsync(ulong.Parse(messageVar.Value));
                }
                catch { noticeMessage = null; }

                string title;
                try { title = GetFirstData($"{channel.Guild.Id}", $"Notices-Title-{channel.Id}").Value; }
                catch { title = ""; }
                try { title = Base64Decode(title); } catch { }

                string content;
                try { content = GetFirstData($"{channel.Guild.Id}", $"Notices-Content-{channel.Id}").Value; }
                catch { content = ""; }
                try { content = Base64Decode(content); } catch { }

                string normalText;
                try { normalText = GetFirstData($"{channel.Guild.Id}", $"Notices-NormalText-{channel.Id}").Value; }
                catch { normalText = ""; }
                try { normalText = Base64Decode(normalText); } catch { }

                string footer;
                try { footer = GetFirstData($"{channel.Guild.Id}", $"Notices-Footer-{channel.Id}").Value; }
                catch { footer = ""; }
                try { footer = Base64Decode(footer); } catch { }

                string imageUrl;
                try { imageUrl = GetFirstData($"{channel.Guild.Id}", $"Notices-ImageURL-{channel.Id}").Value; }
                catch { imageUrl = ""; }
                try { imageUrl = Base64Decode(imageUrl); } catch { }

                string thumbnailUrl;
                try { thumbnailUrl = GetFirstData($"{channel.Guild.Id}", $"Notices-ThumbnailURL-{channel.Id}").Value; }
                catch { thumbnailUrl = ""; }
                try { thumbnailUrl = Base64Decode(thumbnailUrl); } catch { }

                string largeImageUrl;
                try { largeImageUrl = GetFirstData($"{channel.Guild.Id}", $"Notices-LargeImageURL-{channel.Id}").Value; }
                catch { largeImageUrl = ""; }
                try { largeImageUrl = Base64Decode(largeImageUrl); } catch { }

                string footerImageUrl;
                try { footerImageUrl = GetFirstData($"{channel.Guild.Id}", $"Notices-FooterImageURL-{channel.Id}").Value; }
                catch { footerImageUrl = ""; }
                try { footerImageUrl = Base64Decode(footerImageUrl); } catch { }

                string colourString;
                try { colourString = GetFirstData($"{channel.Guild.Id}", $"Notices-Colour-{channel.Id}").Value; }
                catch { colourString = "255 255 255"; }
                byte r = byte.Parse(colourString.Split(" ").ToArray()[0]);
                byte g = byte.Parse(colourString.Split(" ").ToArray()[1]);
                byte b = byte.Parse(colourString.Split(" ").ToArray()[2]);
                Color colour = new Color(r, g, b);

                EmbedBuilder embed = new EmbedBuilder();
                if (title != "") embed.WithAuthor(new EmbedAuthorBuilder().WithName(title));
                if (title == "" && imageUrl != "") embed.WithAuthor(new EmbedAuthorBuilder().WithName("Title required for image!"));
                if (imageUrl != "") embed.WithAuthor(embed.Author.WithIconUrl(imageUrl));

                if (footer != "") embed.WithFooter(new EmbedFooterBuilder().WithText(footer));
                if (footer == "" && footerImageUrl != "") embed.WithFooter(new EmbedFooterBuilder().WithText("Footer required for image!"));
                if (footerImageUrl != "") embed.WithFooter(embed.Footer.WithIconUrl(footerImageUrl));

                if (content != "") embed.WithDescription(content);
                if (thumbnailUrl != "") embed.WithThumbnailUrl(thumbnailUrl);
                if (largeImageUrl != "") embed.WithImageUrl(largeImageUrl);
                embed.WithColor(colour);

                IUserMessage sent = await channel.SendMessageAsync(normalText, embed: embed.Build());

                if (save)
                {
                    await sent.PinAsync();
                    DeleteData($"{channel.Guild.Id}", $"Notices-Message-{channel.Id}");
                    SaveData($"{channel.Guild.Id}", $"Notices-Message-{channel.Id}", $"{sent.Id}");
                }
                else
                {
                    SaveData($"{channel.Guild.Id}", $"Notices-ExcludedMessage-{channel.Id}", $"{sent.Id}");
                }

                try { await noticeMessage.DeleteAsync(); } catch { }

                #region Delete Similar

                IEnumerable<IMessage> messages = await channel.GetMessagesAsync().FlattenAsync();
                List<IMessage> botMessages = messages.Where(x => x.Author.Id == Client.CurrentUser.Id).ToList();
                List<IMessage> embeddedMessages = botMessages.Where(x => x.Embeds.Count > 0).ToList();
                List<IMessage> toDelete = new List<IMessage>();

                foreach (IMessage message in embeddedMessages)
                {
                    bool same = true;

                    IEmbed mEmbed = message.Embeds.First();

                    if (mEmbed.Author.HasValue)
                    {
                        try
                        {
                            if (mEmbed.Author.Value.IconUrl != embed.Author.IconUrl) same = false;
                            if (mEmbed.Author.Value.Name != embed.Author.Name) same = false;
                        }
                        catch { same = false; }
                    }
                    else
                    {
                        if (embed.Author != null) same = false;
                    }

                    if (mEmbed.Description != embed.Description) same = false;
                    if (mEmbed.Color != embed.Color) same = false;

                    if (same) toDelete.Add(message);
                }

                if (toDelete.Count > 0)
                {
                    List<Data> excluded = GetData(channel.Guild.Id.ToString(), $"Notices-ExcludedMessage-{channel.Id}");
                    excluded.AddRange(GetData(channel.Guild.Id.ToString(), $"Notices-Message-{channel.Id}"));
                    foreach (Data data in excluded) toDelete.RemoveAll(x => x.Id.ToString() == data.Value);
                }
                await channel.DeleteMessagesAsync(toDelete);

                #endregion Delete Similar
            }
            else
            {
                try
                {
                    Data messageVar = GetFirstData($"{channel.Guild.Id}", $"Notices-Message-{channel.Id}");
                    IMessage message = await channel.GetMessageAsync(ulong.Parse(messageVar.Value));
                    await message.DeleteAsync();
                }
                catch {
                }
            }
        }
    }

    [Group("Notice"), Alias("Notification", "Notify")]
    public class NoticeMessageCommands : ModuleBase<SocketCommandContext>
    {
        public static string HelpContent =
                "help - Show this list\n" +
                "about - Display feature information\n\n" +
                "title [channel] [message | none] - Set the title of the notice\n" +
                "footer [channel] [message | none] - Set the footer of the notice\n" +
                "content [channel] [message | none] - Set the content of the notice\n" +
                "normalText [channel] [message | none] - Set the text outside of the notice embed\n" +
                "image [channel] [url | none] - Set the image URL of the notice\n" +
                "icon [channel] [url | none] - Set the icon URL of the notice\n" +
                "thumbnail [channel] [url | none] - Set the thumbnail URL of the notice\n" +
                "footerImage [channel] [url | none] - Set the footer URL of the notice\n" +
                "colour [channel] [R] [G] [B] - Set the colour of the notice\n\n" +
                "delay [channel] [timespan] - Set how long after a conversaiton the notice will be posted\n" +
                "send - Send the notice of this channel as a normal message\n" +
                "duplicate [from channel] [to channel] - Duplicate a notice from one channel to another\n\n" +
                "on [channel] - Enable the notice in a channel\n" +
                "off [channel] - Disable the notice in a channel";

        [Command("Help")]
        public async Task Help()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel Notices", HelpContent, $"Prefix these commands with {prefix}notice"));
        }

        [Command("")]
        public async Task Empty()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel Notices", HelpContent, $"Prefix these commands with {prefix}notice"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel Notices", "This feature keeps a customisable message at the bottom of a channel."));
        }

        [Command("SetTitle"), Alias("Title")]
        public async Task SetTitle(ITextChannel channel, [Remainder] string content)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (content.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-Title-{channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Title removed"));
                }
                else if (content.Length <= 500)
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-Title-{channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-Title-{channel.Id}", $"{Base64Encode(content)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Title set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Title can not exceed 500 characters")); }

                NoticeMessage noticeMessage = new NoticeMessage();
                await noticeMessage.Update(channel);
            }
        }

        [Command("SetContent"), Alias("Content")]
        public async Task SetContent(ITextChannel channel, [Remainder] string content)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (content.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-Content-{channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Content removed"));
                }
                else if (content.Length <= 1500)
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-Content-{channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-Content-{channel.Id}", $"{Base64Encode(content)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Content set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Content can not exceed 1500 characters")); }

                NoticeMessage noticeMessage = new NoticeMessage();
                await noticeMessage.Update(channel);
            }
        }

        [Command("SetFooter"), Alias("Footer")]
        public async Task SetFooter(ITextChannel channel, [Remainder] string content)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (content.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-Footer-{channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer removed"));
                }
                else if (content.Length <= 1500)
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-Footer-{channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-Footer-{channel.Id}", $"{Base64Encode(content)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Footer can not exceed 1500 characters")); }

                NoticeMessage noticeMessage = new NoticeMessage();
                await noticeMessage.Update(channel);
            }
        }

        [Command("SetColour"), Alias("SetColor", "Colour", "Color")]
        public async Task SetColour(ITextChannel channel, int r, int g, int b)
        {
            if (Permission(Context.User, Context.Channel))
            {
                Color colour;
                try { colour = new Color(r, g, b); }
                catch { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid colour", "[Pick colour](https://www.rapidtables.com/web/color/RGB_Color.html)\n[Support Discord](https://discord.gg/WsxqABZ)")); return; }

                DeleteData($"{Context.Guild.Id}", $"Notices-Colour-{channel.Id}");
                SaveData($"{Context.Guild.Id}", $"Notices-Colour-{channel.Id}", $"{r} {g} {b}");
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Colour set", "Preview this colour in this embed.").ToEmbedBuilder().WithColor(colour).Build());

                NoticeMessage noticeMessage = new NoticeMessage();
                await noticeMessage.Update(channel);
            }
        }

        [Command("SetIcon"), Alias("Icon", "SetAvatar", "Avatar")]
        public async Task SetIcon(ITextChannel channel, string imageUrl)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (imageUrl.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-ImageURL-{channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Icon removed"));
                }
                else if (imageUrl.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-ImageURL-{channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-ImageURL-{channel.Id}", $"{Base64Encode(imageUrl)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Icon URL set", "Note that an invalid icon URL may result in the message not being sent at all"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Icon URL can not exceed 1000 characters")); }

                NoticeMessage noticeMessage = new NoticeMessage();
                await noticeMessage.Update(channel);
            }
        }

        [Command("SetImage"), Alias("Image")]
        public async Task SetImage(ITextChannel channel, string imageUrl)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (imageUrl.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-LargeImageURL-{channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Image removed"));
                }
                else if (imageUrl.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-LargeImageURL-{channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-LargeImageURL-{channel.Id}", $"{Base64Encode(imageUrl)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Image URL set", "Note that an invalid image URL may result in the message not being sent at all"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Image URL can not exceed 1000 characters")); }

                NoticeMessage noticeMessage = new NoticeMessage();
                await noticeMessage.Update(channel);
            }
        }

        [Command("SetThumbnail"), Alias("Thumbnail")]
        public async Task SetThumbnail(ITextChannel channel, string imageUrl)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (imageUrl.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-ThumbnailURL-{channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Thumbnail removed"));
                }
                else if (imageUrl.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-ThumbnailURL-{channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-ThumbnailURL-{channel.Id}", $"{Base64Encode(imageUrl)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Thumbnail URL set", "Note that an invalid thumbnail URL may result in the message not being sent at all"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Thumbnail URL can not exceed 1000 characters")); }

                NoticeMessage noticeMessage = new NoticeMessage();
                await noticeMessage.Update(channel);
            }
        }

        [Command("SetFooterImage"), Alias("FooterImage")]
        public async Task SetFooterUrl(ITextChannel channel, [Remainder] string imageUrl)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (imageUrl.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-FooterImageURL-{channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer image removed"));
                }
                else if (imageUrl.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-FooterImageURL-{channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-FooterImageURL-{channel.Id}", $"{Base64Encode(imageUrl)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer image set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Footer image URL can not exceed 1000 characters")); }

                NoticeMessage noticeMessage = new NoticeMessage();
                await noticeMessage.Update(channel);
            }
        }

        [Command("SetDelay"), Alias("Time", "SetTime", "Delay")]
        public async Task SetDelay(ITextChannel channel, TimeSpan delay)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (delay <= TimeSpan.FromMinutes(30) && delay >= TimeSpan.FromSeconds(1))
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-Delay-{channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-Delay-{channel.Id}", $"{delay}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Delay set", $"{DisplayTimespan(delay)} after a conversation the notice will be posted"));

                    NoticeMessage noticeMessage = new NoticeMessage();
                    await noticeMessage.Update(channel);
                }
                else
                {
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid timespan", "The timespan can not be less than 1 second or exceed 30 minutes."));
                }
            }
        }

        [Command("SetNormalText"), Alias("NormalText")]
        public async Task SetNormalText(ITextChannel channel, [Remainder] string content)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (content.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-NormalText-{channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Normal text removed"));
                }
                else if (content.Length <= 1500)
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-NormalText-{channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-NormalText-{channel.Id}", $"{Base64Encode(content)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Normal text set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Normal text can not exceed 1500 characters")); }

                NoticeMessage noticeMessage = new NoticeMessage();
                await noticeMessage.Update(channel);
            }
        }

        [Command("Duplicate"), Alias("Move", "Copy")]
        public async Task Duplicate(ITextChannel from, ITextChannel to)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(to, new[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.ManageMessages }, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-Title-{to.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-Content-{to.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-Colour-{to.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-ImageURL-{to.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-ThumbnailURL-{to.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-LargeImageURL-{to.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-Delay-{to.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-Footer-{to.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-FooterImageURL-{to.Id}");
                    DeleteData(Context.Guild.Id.ToString(), "Notices-Channel", to.Id.ToString());

                    Data data = GetFirstData(Context.Guild.Id.ToString(), $"Notices-Title-{from.Id}");
                    if (data != null) SaveData(Context.Guild.Id.ToString(), $"Notices-Title-{to.Id}", data.Value);

                    data = GetFirstData(Context.Guild.Id.ToString(), $"Notices-Content-{from.Id}");
                    if (data != null) SaveData(Context.Guild.Id.ToString(), $"Notices-Content-{to.Id}", data.Value);

                    data = GetFirstData(Context.Guild.Id.ToString(), $"Notices-Colour-{from.Id}");
                    if (data != null) SaveData(Context.Guild.Id.ToString(), $"Notices-Colour-{to.Id}", data.Value);

                    data = GetFirstData(Context.Guild.Id.ToString(), $"Notices-ImageURL-{from.Id}");
                    if (data != null) SaveData(Context.Guild.Id.ToString(), $"Notices-ImageURL-{to.Id}", data.Value);

                    data = GetFirstData(Context.Guild.Id.ToString(), $"Notices-ThumbnailURL-{from.Id}");
                    if (data != null) SaveData(Context.Guild.Id.ToString(), $"Notices-ThumbnailURL-{to.Id}", data.Value);

                    data = GetFirstData(Context.Guild.Id.ToString(), $"Notices-LargeImageURL-{from.Id}");
                    if (data != null) SaveData(Context.Guild.Id.ToString(), $"Notices-LargeImageURL-{to.Id}", data.Value);

                    data = GetFirstData(Context.Guild.Id.ToString(), $"Notices-Footer-{from.Id}");
                    if (data != null) SaveData(Context.Guild.Id.ToString(), $"Notices-Footer-{to.Id}", data.Value);

                    data = GetFirstData(Context.Guild.Id.ToString(), $"Notices-FooterImageURL-{from.Id}");
                    if (data != null) SaveData(Context.Guild.Id.ToString(), $"Notices-FooterImageURL-{to.Id}", data.Value);

                    data = GetFirstData(Context.Guild.Id.ToString(), $"Notices-Delay-{from.Id}");
                    if (data != null) SaveData(Context.Guild.Id.ToString(), $"Notices-Delay-{to.Id}", data.Value);

                    SaveData(Context.Guild.Id.ToString(), "Notices-Channel", to.Id.ToString());

                    NoticeMessage nm = new NoticeMessage();
                    await nm.Update(to);

                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Notice duplicated", $"Notice settings for {from.Mention} were copied to {to.Mention}"));
                }
            }
        }

        [Command("Send")]
        public async Task Send()
        {
            NoticeMessage nm = new NoticeMessage();
            await nm.Update(Context.Channel as ITextChannel, false);
        }

        [Command("On"), Alias("Enable")]
        public async Task On(ITextChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(channel, new[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.ManageMessages }, Context.Channel))
                {
                    DeleteData($"{Context.Guild.Id}", "Notices-Channel", $"{channel.Id}");
                    SaveData($"{Context.Guild.Id}", "Notices-Channel", $"{channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Notice enabled", "Configure the notice using other commands in this category"));

                    NoticeMessage noticeMessage = new NoticeMessage();
                    await noticeMessage.Update(channel);
                }
            }
        }

        [Command("Off"), Alias("Disable")]
        public async Task Off(ITextChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData($"{Context.Guild.Id}", "Notices-Channel", $"{channel.Id}");
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Notice disabled"));

                NoticeMessage noticeMessage = new NoticeMessage();
                await noticeMessage.Update(channel);
            }
        }
    }
}