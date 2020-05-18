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

namespace Utili
{
    class NoticeMessage
    {
        private static List<(ulong, int)> MessageList = new List<(ulong, int)>();

        public async Task NoticeMessage_MessageReceived(SocketMessage MessageParam)
        {
            var Message = MessageParam as SocketUserMessage;
            var Context = new SocketCommandContext(Program.Client, Message);

            if(Message.Author.Id == Client.CurrentUser.Id)
            {
                await Task.Delay(5000);
                var NewMSG = await Context.Channel.GetMessageAsync(Message.Id);
                if (NewMSG.IsPinned) return;
            }

            int ThisNumber;
            try { ThisNumber = MessageList.OrderBy(x => x.Item2).Last(x => x.Item1 == Context.Channel.Id).Item2 + 1; }
            catch { ThisNumber = 0; }

            MessageList.RemoveAll(x => x.Item1 == Context.Channel.Id);
            MessageList.Add((Context.Channel.Id, ThisNumber));

            if(GetData($"{Context.Guild.Id}", $"Notices-Channel", $"{Context.Channel.Id}").Count > 0)
            {
                TimeSpan Delay = TimeSpan.FromSeconds(15);
                try { Delay = TimeSpan.Parse(GetData($"{Context.Guild.Id}", $"Notices-Delay-{Context.Channel.Id}").First().Value); }
                catch { }

                await Task.Delay(int.Parse(Math.Ceiling(Delay.TotalMilliseconds).ToString()));

                int NewNumber;
                try { NewNumber = MessageList.OrderBy(x => x.Item2).Last(x => x.Item1 == Context.Channel.Id).Item2; }
                catch { NewNumber = 0; }

                if (ThisNumber == NewNumber) await Update(Context.Channel as ITextChannel, true, true);
                else return;
            }
        }

        public async Task Update(ITextChannel Channel, bool Save = true, bool KnownOn = false)
        {
            if (!KnownOn) KnownOn = GetData($"{Channel.Guild.Id}", $"Notices-Channel", $"{Channel.Id}").Count > 0;
            if (KnownOn || !Save)
            {
                IMessage NoticeMessage;
                try
                {
                    Data MessageVar = GetData($"{Channel.Guild.Id}", $"Notices-Message-{Channel.Id}").First();
                    NoticeMessage = await Channel.GetMessageAsync(ulong.Parse(MessageVar.Value));
                }
                catch { NoticeMessage = null; }

                string Title;
                try { Title = GetData($"{Channel.Guild.Id}", $"Notices-Title-{Channel.Id}").First().Value; }
                catch { Title = ""; }
                try { Title = Base64Decode(Title); } catch { };

                string Content;
                try { Content = GetData($"{Channel.Guild.Id}", $"Notices-Content-{Channel.Id}").First().Value; }
                catch { Content = ""; }
                try { Content = Base64Decode(Content); } catch { };

                string NormalText;
                try { NormalText = GetData($"{Channel.Guild.Id}", $"Notices-NormalText-{Channel.Id}").First().Value; }
                catch { NormalText = ""; }
                try { NormalText = Base64Decode(NormalText); } catch { };

                string Footer;
                try { Footer = GetData($"{Channel.Guild.Id}", $"Notices-Footer-{Channel.Id}").First().Value; }
                catch { Footer = ""; }
                try { Footer = Base64Decode(Footer); } catch { };

                string ImageURL;
                try { ImageURL = GetData($"{Channel.Guild.Id}", $"Notices-ImageURL-{Channel.Id}").First().Value; }
                catch { ImageURL = ""; }
                try { ImageURL = Base64Decode(ImageURL); } catch { };

                string ThumbnailURL;
                try { ThumbnailURL = GetData($"{Channel.Guild.Id}", $"Notices-ThumbnailURL-{Channel.Id}").First().Value; }
                catch { ThumbnailURL = ""; }
                try { ThumbnailURL = Base64Decode(ThumbnailURL); } catch { };

                string LargeImageURL;
                try { LargeImageURL = GetData($"{Channel.Guild.Id}", $"Notices-LargeImageURL-{Channel.Id}").First().Value; }
                catch { LargeImageURL = ""; }
                try { LargeImageURL = Base64Decode(LargeImageURL); } catch { };

                string FooterImageURL;
                try { FooterImageURL = GetData($"{Channel.Guild.Id}", $"Notices-FooterImageURL-{Channel.Id}").First().Value; }
                catch { FooterImageURL = ""; }
                try { FooterImageURL = Base64Decode(FooterImageURL); } catch { };

                string ColourString;
                try { ColourString = GetData($"{Channel.Guild.Id}", $"Notices-Colour-{Channel.Id}").First().Value; }
                catch { ColourString = "255 255 255"; }
                byte R = byte.Parse(ColourString.Split(" ").ToArray()[0]);
                byte G = byte.Parse(ColourString.Split(" ").ToArray()[1]);
                byte B = byte.Parse(ColourString.Split(" ").ToArray()[2]);
                Color Colour = new Color(R, G, B);

                EmbedBuilder Embed = new EmbedBuilder();
                if (Title != "") Embed.WithAuthor(new EmbedAuthorBuilder().WithName(Title));
                if(Title == "" && ImageURL != "") Embed.WithAuthor(new EmbedAuthorBuilder().WithName("Title required for image!"));
                if (ImageURL != "") Embed.WithAuthor(Embed.Author.WithIconUrl(ImageURL));

                if (Footer != "") Embed.WithFooter(new EmbedFooterBuilder().WithText(Footer));
                if (Footer == "" && FooterImageURL != "") Embed.WithFooter(new EmbedFooterBuilder().WithText("Footer required for image!"));
                if (FooterImageURL != "") Embed.WithFooter(Embed.Footer.WithIconUrl(FooterImageURL));

                if (Content != "") Embed.WithDescription(Content);
                if (ThumbnailURL != "") Embed.WithThumbnailUrl(ThumbnailURL);
                if (LargeImageURL != "") Embed.WithImageUrl(LargeImageURL);
                Embed.WithColor(Colour);

                var Sent = await Channel.SendMessageAsync(NormalText, embed: Embed.Build());

                if (Save)
                {
                    await Sent.PinAsync();
                    DeleteData($"{Channel.Guild.Id}", $"Notices-Message-{Channel.Id}");
                    SaveData($"{Channel.Guild.Id}", $"Notices-Message-{Channel.Id}", $"{Sent.Id}");
                }
                else
                {
                    SaveData($"{Channel.Guild.Id}", $"Notices-ExcludedMessage-{Channel.Id}", $"{Sent.Id}");
                }

                try { await NoticeMessage.DeleteAsync(); } catch { }

                #region Delete Similar

                var Messages = await Channel.GetMessagesAsync(100).FlattenAsync();
                List<IMessage> BotMessages = Messages.Where(x => x.Author.Id == Client.CurrentUser.Id).ToList();
                List<IMessage> EmbeddedMessages = BotMessages.Where(x => x.Embeds.Count > 0).ToList();
                List<IMessage> ToDelete = new List<IMessage>();

                foreach (var Message in EmbeddedMessages)
                {
                    bool Same = true;

                    var MEmbed = Message.Embeds.First();

                    if (MEmbed.Author.HasValue)
                    {
                        try
                        {
                            if (MEmbed.Author.Value.IconUrl != Embed.Author.IconUrl) Same = false;
                            if (MEmbed.Author.Value.Name != Embed.Author.Name) Same = false;
                        }
                        catch { Same = false; }
                    }
                    else
                    {
                        if (Embed.Author != null) Same = false;
                    }

                    if (MEmbed.Description != Embed.Description) Same = false;
                    if (MEmbed.Color != Embed.Color) Same = false;

                    if (Same) ToDelete.Add(Message);
                }

                if (ToDelete.Count > 0)
                {
                    var Excluded = GetData(Channel.Guild.Id.ToString(), $"Notices-ExcludedMessage-{Channel.Id}");
                    Excluded.AddRange(GetData(Channel.Guild.Id.ToString(), $"Notices-Message-{Channel.Id}"));
                    foreach (var Data in Excluded) ToDelete.RemoveAll(x => x.Id.ToString() == Data.Value);
                }
                await Channel.DeleteMessagesAsync(ToDelete);

                #endregion
            }
            else
            {
                try 
                { 
                    Data MessageVar = GetData($"{Channel.Guild.Id}", $"Notices-Message-{Channel.Id}").First();
                    IMessage Message = await Channel.GetMessageAsync(ulong.Parse(MessageVar.Value));
                    await Message.DeleteAsync();
                } 
                catch { return; }
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
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel Notices", HelpContent, $"Prefix these commands with {Prefix}notice"));
        }

        [Command("")]
        public async Task Empty()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel Notices", HelpContent, $"Prefix these commands with {Prefix}notice"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel Notices", "This feature keeps a customisable message at the bottom of a channel."));
        }

        [Command("SetTitle"), Alias("Title")]
        public async Task SetTitle(ITextChannel Channel, [Remainder] string Content)
        {
            if(Permission(Context.User, Context.Channel))
            {
                if(Content.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-Title-{Channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Title removed"));
                }
                else if(Content.Length <= 500)
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-Title-{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-Title-{Channel.Id}", $"{Base64Encode(Content)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Title set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Title can not exceed 500 characters")); }

                NoticeMessage NoticeMessage = new NoticeMessage();
                await NoticeMessage.Update(Channel);
            }
        }

        [Command("SetContent"), Alias("Content")]
        public async Task SetContent(ITextChannel Channel, [Remainder] string Content)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (Content.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-Content-{Channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Content removed"));
                }
                else if (Content.Length <= 1500)
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-Content-{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-Content-{Channel.Id}", $"{Base64Encode(Content)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Content set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Content can not exceed 1500 characters")); }

                NoticeMessage NoticeMessage = new NoticeMessage();
                await NoticeMessage.Update(Channel);
            }
        }

        [Command("SetFooter"), Alias("Footer")]
        public async Task SetFooter(ITextChannel Channel, [Remainder] string Content)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (Content.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-Footer-{Channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer removed"));
                }
                else if (Content.Length <= 1500)
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-Footer-{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-Footer-{Channel.Id}", $"{Base64Encode(Content)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Footer can not exceed 1500 characters")); }

                NoticeMessage NoticeMessage = new NoticeMessage();
                await NoticeMessage.Update(Channel);
            }
        }

        [Command("SetColour"), Alias("SetColor", "Colour", "Color")]
        public async Task SetColour(ITextChannel Channel, int R, int G, int B)
        {
            if (Permission(Context.User, Context.Channel))
            {
                Color Colour;
                try { Colour = new Color(R, G, B); }
                catch { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid colour", "[Pick colour](https://www.rapidtables.com/web/color/RGB_Color.html)\n[Support Discord](https://discord.gg/WsxqABZ)")); return; }
                    
                DeleteData($"{Context.Guild.Id}", $"Notices-Colour-{Channel.Id}");
                SaveData($"{Context.Guild.Id}", $"Notices-Colour-{Channel.Id}", $"{R} {G} {B}");
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Colour set", "Preview this colour in this embed.").ToEmbedBuilder().WithColor(Colour).Build());

                NoticeMessage NoticeMessage = new NoticeMessage();
                await NoticeMessage.Update(Channel);
            }
        }

        [Command("SetIcon"), Alias("Icon", "SetAvatar", "Avatar")]
        public async Task SetIcon(ITextChannel Channel, string ImageURL)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (ImageURL.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-ImageURL-{Channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Icon removed"));
                }
                else if (ImageURL.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-ImageURL-{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-ImageURL-{Channel.Id}", $"{Base64Encode(ImageURL)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Icon URL set", "Note that an invalid icon URL may result in the message not being sent at all"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Icon URL can not exceed 1000 characters")); }

                NoticeMessage NoticeMessage = new NoticeMessage();
                await NoticeMessage.Update(Channel);
            }
        }

        [Command("SetImage"), Alias("Image")]
        public async Task SetImage(ITextChannel Channel, string ImageURL)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (ImageURL.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-LargeImageURL-{Channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Image removed"));
                }
                else if (ImageURL.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-LargeImageURL-{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-LargeImageURL-{Channel.Id}", $"{Base64Encode(ImageURL)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Image URL set", "Note that an invalid image URL may result in the message not being sent at all"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Image URL can not exceed 1000 characters")); }

                NoticeMessage NoticeMessage = new NoticeMessage();
                await NoticeMessage.Update(Channel);
            }
        }

        [Command("SetThumbnail"), Alias("Thumbnail")]
        public async Task SetThumbnail(ITextChannel Channel, string ImageURL)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (ImageURL.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-ThumbnailURL-{Channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Thumbnail removed"));
                }
                else if (ImageURL.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-ThumbnailURL-{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-ThumbnailURL-{Channel.Id}", $"{Base64Encode(ImageURL)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Thumbnail URL set", "Note that an invalid thumbnail URL may result in the message not being sent at all"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Thumbnail URL can not exceed 1000 characters")); }

                NoticeMessage NoticeMessage = new NoticeMessage();
                await NoticeMessage.Update(Channel);
            }
        }

        [Command("SetFooterImage"), Alias("FooterImage")]
        public async Task SetFooterURL(ITextChannel Channel, [Remainder] string ImageURL)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (ImageURL.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-FooterImageURL-{Channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer image removed"));
                }
                else if (ImageURL.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-FooterImageURL-{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-FooterImageURL-{Channel.Id}", $"{Base64Encode(ImageURL)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer image set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Footer image URL can not exceed 1000 characters")); }

                NoticeMessage NoticeMessage = new NoticeMessage();
                await NoticeMessage.Update(Channel);
            }
        }

        [Command("SetDelay"), Alias("Time", "SetTime", "Delay")]
        public async Task SetDelay(ITextChannel Channel, TimeSpan Delay)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if(Delay <= TimeSpan.FromMinutes(30) && Delay >= TimeSpan.FromSeconds(1))
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-Delay-{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-Delay-{Channel.Id}", $"{Delay}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Delay set", $"{DisplayTimespan(Delay)} after a conversation the notice will be posted"));

                    NoticeMessage NoticeMessage = new NoticeMessage();
                    await NoticeMessage.Update(Channel);
                }
                else
                {
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid timespan", "The timespan can not be less than 1 second or exceed 30 minutes."));
                }
            }
        }

        [Command("SetNormalText"), Alias("NormalText")]
        public async Task SetNormalText(ITextChannel Channel, [Remainder] string Content)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (Content.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-NormalText-{Channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Normal text removed"));
                }
                else if (Content.Length <= 1500)
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-NormalText-{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-NormalText-{Channel.Id}", $"{Base64Encode(Content)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Normal text set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Normal text can not exceed 1500 characters")); }

                NoticeMessage NoticeMessage = new NoticeMessage();
                await NoticeMessage.Update(Channel);
            }
        }

        [Command("Duplicate"), Alias("Move", "Copy")]
        public async Task Duplicate(ITextChannel From, ITextChannel To)
        {
            if(Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(To, new ChannelPermission[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.ManageMessages }, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-Title-{To.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-Content-{To.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-Colour-{To.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-ImageURL-{To.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-ThumbnailURL-{To.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-LargeImageURL-{To.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-Delay-{To.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-Footer-{To.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-FooterImageURL-{To.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Notices-Channel", To.Id.ToString());

                    var Data = GetData(Context.Guild.Id.ToString(), $"Notices-Title-{From.Id}");
                    if (Data.Count != 0) SaveData(Context.Guild.Id.ToString(), $"Notices-Title-{To.Id}", Data.First().Value);

                    Data = GetData(Context.Guild.Id.ToString(), $"Notices-Content-{From.Id}");
                    if (Data.Count != 0) SaveData(Context.Guild.Id.ToString(), $"Notices-Content-{To.Id}", Data.First().Value);

                    Data = GetData(Context.Guild.Id.ToString(), $"Notices-Colour-{From.Id}");
                    if (Data.Count != 0) SaveData(Context.Guild.Id.ToString(), $"Notices-Colour-{To.Id}", Data.First().Value);

                    Data = GetData(Context.Guild.Id.ToString(), $"Notices-ImageURL-{From.Id}");
                    if (Data.Count != 0) SaveData(Context.Guild.Id.ToString(), $"Notices-ImageURL-{To.Id}", Data.First().Value);

                    Data = GetData(Context.Guild.Id.ToString(), $"Notices-ThumbnailURL-{From.Id}");
                    if (Data.Count != 0) SaveData(Context.Guild.Id.ToString(), $"Notices-ThumbnailURL-{To.Id}", Data.First().Value);

                    Data = GetData(Context.Guild.Id.ToString(), $"Notices-LargeImageURL-{From.Id}");
                    if (Data.Count != 0) SaveData(Context.Guild.Id.ToString(), $"Notices-LargeImageURL-{To.Id}", Data.First().Value);

                    Data = GetData(Context.Guild.Id.ToString(), $"Notices-Footer-{From.Id}");
                    if (Data.Count != 0) SaveData(Context.Guild.Id.ToString(), $"Notices-Footer-{To.Id}", Data.First().Value);

                    Data = GetData(Context.Guild.Id.ToString(), $"Notices-FooterImageURL-{From.Id}");
                    if (Data.Count != 0) SaveData(Context.Guild.Id.ToString(), $"Notices-FooterImageURL-{To.Id}", Data.First().Value);

                    Data = GetData(Context.Guild.Id.ToString(), $"Notices-Delay-{From.Id}");
                    if (Data.Count != 0) SaveData(Context.Guild.Id.ToString(), $"Notices-Delay-{To.Id}", Data.First().Value);

                    SaveData(Context.Guild.Id.ToString(), $"Notices-Channel", To.Id.ToString());

                    NoticeMessage NM = new NoticeMessage();
                    await NM.Update(To);

                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Notice duplicated", $"Notice settings for {From.Mention} were copied to {To.Mention}"));
                } 
            }
        }

        [Command("Send")]
        public async Task Send()
        {
            NoticeMessage NM = new NoticeMessage();
            await NM.Update(Context.Channel as ITextChannel, false);
        }

        [Command("On"), Alias("Enable")]
        public async Task On(ITextChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(Channel, new ChannelPermission[]{ ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.ManageMessages }, Context.Channel))
                {
                    DeleteData($"{Context.Guild.Id}", $"Notices-Channel", $"{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"Notices-Channel", $"{Channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Notice enabled", "Configure the notice using other commands in this category"));

                    NoticeMessage NoticeMessage = new NoticeMessage();
                    await NoticeMessage.Update(Channel);
                }
            }
        }

        [Command("Off"), Alias("Disable")]
        public async Task Off(ITextChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData($"{Context.Guild.Id}", $"Notices-Channel", $"{Channel.Id}");
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Notice disabled"));

                NoticeMessage NoticeMessage = new NoticeMessage();
                await NoticeMessage.Update(Channel);
            }
        }
    }
}
