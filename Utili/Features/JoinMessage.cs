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
    class JoinMessage
    {
        public async Task SendMessage(ITextChannel Channel)
        {
            string Title;
            try { Title = GetData($"{Channel.Guild.Id}", $"JoinMessage-Title-{Channel.Id}").First().Value; }
            catch { Title = ""; }
            try { Title = Base64Decode(Title); } catch { };

            string Content;
            try { Content = GetData($"{Channel.Guild.Id}", $"JoinMessage-Content-{Channel.Id}").First().Value; }
            catch { Content = ""; }
            try { Content = Base64Decode(Content); } catch { };

            string NormalText;
            try { NormalText = GetData($"{Channel.Guild.Id}", $"JoinMessage-NormalText-{Channel.Id}").First().Value; }
            catch { NormalText = ""; }
            try { NormalText = Base64Decode(NormalText); } catch { };

            string Footer;
            try { Footer = GetData($"{Channel.Guild.Id}", $"JoinMessage-Footer-{Channel.Id}").First().Value; }
            catch { Footer = ""; }
            try { Footer = Base64Decode(Footer); } catch { };

            string ImageURL;
            try { ImageURL = GetData($"{Channel.Guild.Id}", $"JoinMessage-ImageURL-{Channel.Id}").First().Value; }
            catch { ImageURL = ""; }
            try { ImageURL = Base64Decode(ImageURL); } catch { };

            string ThumbnailURL;
            try { ThumbnailURL = GetData($"{Channel.Guild.Id}", $"JoinMessage-ThumbnailURL-{Channel.Id}").First().Value; }
            catch { ThumbnailURL = ""; }
            try { ThumbnailURL = Base64Decode(ThumbnailURL); } catch { };

            string LargeImageURL;
            try { LargeImageURL = GetData($"{Channel.Guild.Id}", $"JoinMessage-LargeImageURL-{Channel.Id}").First().Value; }
            catch { LargeImageURL = ""; }
            try { LargeImageURL = Base64Decode(LargeImageURL); } catch { };

            string FooterImageURL;
            try { FooterImageURL = GetData($"{Channel.Guild.Id}", $"JoinMessage-FooterImageURL-{Channel.Id}").First().Value; }
            catch { FooterImageURL = ""; }
            try { FooterImageURL = Base64Decode(FooterImageURL); } catch { };

            string ColourString;
            try { ColourString = GetData($"{Channel.Guild.Id}", $"JoinMessage-Colour-{Channel.Id}").First().Value; }
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

            await Channel.SendMessageAsync(NormalText, embed: Embed.Build());
        }
    }

    [Group("JoinMessage")]
    public class JoinMessageCommands : ModuleBase<SocketCommandContext>
    {
        public static string HelpContent =
                "help - Show this list\n" +
                "about - Display feature information\n\n" +
                "title [channel] [message | none] - Set the title of the join message\n" +
                "footer [channel] [message | none] - Set the footer of the join message\n" +
                "content [channel] [message | none] - Set the content of the join message\n" +
                "normalText [channel] [message | none] - Set the text outside of the join message embed\n" +
                "image [channel] [url | none] - Set the image URL of the join message\n" +
                "icon [channel] [url | none] - Set the icon URL of the join message\n" +
                "thumbnail [channel] [url | none] - Set the thumbnail URL of the join message\n" +
                "footerImage [channel] [url | none] - Set the footer URL of the join message\n" +
                "colour [channel] [R] [G] [B] - Set the colour of the join message\n\n" +
                "preview - Send a preview the join message\n" +
                "on [channel] - Enable the join message in a channel\n" +
                "off [channel] - Disable the join message in a channel";

        [Command("Help")]
        public async Task Help()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel JoinMessage", HelpContent, $"Prefix these commands with {Prefix}notice"));
        }

        [Command("")]
        public async Task Empty()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel JoinMessage", HelpContent, $"Prefix these commands with {Prefix}notice"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel JoinMessage", "This feature keeps a customisable message at the bottom of a channel."));
        }

        [Command("SetTitle"), Alias("Title")]
        public async Task SetTitle(ITextChannel Channel, [Remainder] string Content)
        {
            if(Permission(Context.User, Context.Channel))
            {
                if(Content.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-Title-{Channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Title removed"));
                }
                else if(Content.Length <= 500)
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-Title-{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"JoinMessage-Title-{Channel.Id}", $"{Base64Encode(Content)}");
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
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-Content-{Channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Content removed"));
                }
                else if (Content.Length <= 1500)
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-Content-{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"JoinMessage-Content-{Channel.Id}", $"{Base64Encode(Content)}");
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
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-Footer-{Channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer removed"));
                }
                else if (Content.Length <= 1500)
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-Footer-{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"JoinMessage-Footer-{Channel.Id}", $"{Base64Encode(Content)}");
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
                    
                DeleteData($"{Context.Guild.Id}", $"JoinMessage-Colour-{Channel.Id}");
                SaveData($"{Context.Guild.Id}", $"JoinMessage-Colour-{Channel.Id}", $"{R} {G} {B}");
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
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-ImageURL-{Channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Icon removed"));
                }
                else if (ImageURL.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-ImageURL-{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"JoinMessage-ImageURL-{Channel.Id}", $"{Base64Encode(ImageURL)}");
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
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-LargeImageURL-{Channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Image removed"));
                }
                else if (ImageURL.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-LargeImageURL-{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"JoinMessage-LargeImageURL-{Channel.Id}", $"{Base64Encode(ImageURL)}");
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
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-ThumbnailURL-{Channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Thumbnail removed"));
                }
                else if (ImageURL.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-ThumbnailURL-{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"JoinMessage-ThumbnailURL-{Channel.Id}", $"{Base64Encode(ImageURL)}");
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
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-FooterImageURL-{Channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer image removed"));
                }
                else if (ImageURL.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-FooterImageURL-{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"JoinMessage-FooterImageURL-{Channel.Id}", $"{Base64Encode(ImageURL)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer image set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Footer image URL can not exceed 1000 characters")); }

                NoticeMessage NoticeMessage = new NoticeMessage();
                await NoticeMessage.Update(Channel);
            }
        }

        [Command("SetNormalText"), Alias("NormalText")]
        public async Task SetNormalText(ITextChannel Channel, [Remainder] string Content)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (Content.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-NormalText-{Channel.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Normal text removed"));
                }
                else if (Content.Length <= 1500)
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-NormalText-{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"JoinMessage-NormalText-{Channel.Id}", $"{Base64Encode(Content)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Normal text set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Normal text can not exceed 1500 characters")); }

                NoticeMessage NoticeMessage = new NoticeMessage();
                await NoticeMessage.Update(Channel);
            }
        }

        [Command("Preview")]
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
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-Channel", $"{Channel.Id}");
                    SaveData($"{Context.Guild.Id}", $"JoinMessage-Channel", $"{Channel.Id}");
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
                DeleteData($"{Context.Guild.Id}", $"JoinMessage-Channel", $"{Channel.Id}");
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Notice disabled"));

                NoticeMessage NoticeMessage = new NoticeMessage();
                await NoticeMessage.Update(Channel);
            }
        }
    }
}
