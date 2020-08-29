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
    internal class JoinMessage
    {
        public async Task JoinMessage_UserJoined(SocketGuildUser User)
        {
            if (DataExists(User.Guild.Id.ToString(), "JoinMessage-Enabled", "true"))
            {
                var JoinMessage = await GetJoinMessageAsync(User.Guild, User);

                string ChannelData;
                try { ChannelData = GetFirstData(User.Guild.Id.ToString(), "JoinMessage-Channel").Value; } catch { ChannelData = "DM"; }
                if (ulong.TryParse(ChannelData, out ulong ChannelID))
                {
                    ITextChannel Channel = User.Guild.GetTextChannel(ChannelID);
                    await Channel.SendMessageAsync(JoinMessage.Item1, embed: JoinMessage.Item2);
                }
                else
                {
                    IDMChannel Channel = await User.GetOrCreateDMChannelAsync();
                    await Channel.SendMessageAsync(JoinMessage.Item1, embed: JoinMessage.Item2);
                }
            }
        }

        public static async Task<(string, Embed)> GetJoinMessageAsync(SocketGuild Guild, SocketUser User)
        {
            string Title;
            try { Title = GetFirstData($"{Guild.Id}", $"JoinMessage-Title").Value; }
            catch { Title = ""; }
            try { Title = Base64Decode(Title).Replace("%user%", $"{User.Username}#{User.Discriminator}"); } catch { };

            string Content;
            try { Content = GetFirstData($"{Guild.Id}", $"JoinMessage-Content").Value; }
            catch { Content = ""; }
            try { Content = Base64Decode(Content).Replace("%user%", $"{User.Mention}"); } catch { };

            string NormalText;
            try { NormalText = GetFirstData($"{Guild.Id}", $"JoinMessage-NormalText").Value; }
            catch { NormalText = ""; }
            try { NormalText = Base64Decode(NormalText).Replace("%user%", $"{User.Mention}"); } catch { };

            string Footer;
            try { Footer = GetFirstData($"{Guild.Id}", $"JoinMessage-Footer").Value; }
            catch { Footer = ""; }
            try { Footer = Base64Decode(Footer).Replace("%user%", $"{User.Username}#{User.Discriminator}"); } catch { };

            string ImageURL;
            try { ImageURL = GetFirstData($"{Guild.Id}", $"JoinMessage-ImageURL").Value; }
            catch { ImageURL = ""; }
            try { ImageURL = Base64Decode(ImageURL); } catch { };
            if (ImageURL == "user") ImageURL = User.GetAvatarUrl();

            string ThumbnailURL;
            try { ThumbnailURL = GetFirstData($"{Guild.Id}", $"JoinMessage-ThumbnailURL").Value; }
            catch { ThumbnailURL = ""; }
            try { ThumbnailURL = Base64Decode(ThumbnailURL); } catch { };
            if (ThumbnailURL == "user") ThumbnailURL = User.GetAvatarUrl();

            string LargeImageURL;
            try { LargeImageURL = GetFirstData($"{Guild.Id}", $"JoinMessage-LargeImageURL").Value; }
            catch { LargeImageURL = ""; }
            try { LargeImageURL = Base64Decode(LargeImageURL); } catch { };
            if (LargeImageURL == "user") LargeImageURL = User.GetAvatarUrl();

            string FooterImageURL;
            try { FooterImageURL = GetFirstData($"{Guild.Id}", $"JoinMessage-FooterImageURL").Value; }
            catch { FooterImageURL = ""; }
            try { FooterImageURL = Base64Decode(FooterImageURL); } catch { };
            if (FooterImageURL == "user") FooterImageURL = User.GetAvatarUrl();

            string ColourString;
            try { ColourString = GetFirstData($"{Guild.Id}", $"JoinMessage-Colour").Value; }
            catch { ColourString = "255 255 255"; }
            byte R = byte.Parse(ColourString.Split(" ").ToArray()[0]);
            byte G = byte.Parse(ColourString.Split(" ").ToArray()[1]);
            byte B = byte.Parse(ColourString.Split(" ").ToArray()[2]);
            Color Colour = new Color(R, G, B);

            EmbedBuilder Embed = new EmbedBuilder();
            if (Title != "") Embed.WithAuthor(new EmbedAuthorBuilder().WithName(Title));
            if (Title == "" && ImageURL != "") Embed.WithAuthor(new EmbedAuthorBuilder().WithName("Title required for image!"));
            if (ImageURL != "") Embed.WithAuthor(Embed.Author.WithIconUrl(ImageURL));

            if (Footer != "") Embed.WithFooter(new EmbedFooterBuilder().WithText(Footer));
            if (Footer == "" && FooterImageURL != "") Embed.WithFooter(new EmbedFooterBuilder().WithText("Footer required for image!"));
            if (FooterImageURL != "") Embed.WithFooter(Embed.Footer.WithIconUrl(FooterImageURL));

            if (Content != "") Embed.WithDescription(Content);
            if (ThumbnailURL != "") Embed.WithThumbnailUrl(ThumbnailURL);
            if (LargeImageURL != "") Embed.WithImageUrl(LargeImageURL);
            Embed.WithColor(Colour);

            return (NormalText, Embed.Build());
        }
    }

    [Group("JoinMessage")]
    public class JoinMessageCommands : ModuleBase<SocketCommandContext>
    {
        public static string HelpContent =
                "help - Show this list\n" +
                "about - Display feature information\n\n" +
                "**Use %user% in a text field to replace it with @user**\n" +
                "title [message | none] - Set the title of the join message\n" +
                "footer [message | none] - Set the footer of the join message\n" +
                "content [message | none] - Set the content of the join message\n" +
                "normalText [message | none] - Set the text outside of the join message embed\n\n" +
                "**Use \"user\" as your image URL to place it with the user's profile picture**\n" +
                "image [url | user | none] - Set the image URL of the join message\n" +
                "icon [url | user | none] - Set the icon URL of the join message\n" +
                "thumbnail [url | user | none] - Set the thumbnail URL of the join message\n" +
                "footerImage [url | user | none] - Set the footer URL of the join message\n" +
                "colour [R] [G] [B] - Set the colour of the join message\n\n" +
                "channel [channel | dm] - Set the channel of the join messages\n" +
                "preview - Send a preview the join message\n" +
                "on - Enable the join message\n" +
                "off - Disable the join message";

        [Command("Help")]
        public async Task Help()
        {
            string Prefix = ".";
            try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Join Message", HelpContent, $"Prefix these commands with {Prefix}joinmessage"));
        }

        [Command("")]
        public async Task Empty()
        {
            string Prefix = ".";
            try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Join Message", HelpContent, $"Prefix these commands with {Prefix}joinmessage"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Join Message", "This feature allows you to send a message in a channel or directly to users when they join the guild"));
        }

        [Command("SetTitle"), Alias("Title")]
        public async Task SetTitle([Remainder] string Content)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (Content.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-Title");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Title removed"));
                }
                else if (Content.Length <= 500)
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-Title");
                    SaveData($"{Context.Guild.Id}", $"JoinMessage-Title", $"{Base64Encode(Content)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Title set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Title can not exceed 500 characters")); }
            }
        }

        [Command("SetContent"), Alias("Content")]
        public async Task SetContent([Remainder] string Content)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (Content.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-Content");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Content removed"));
                }
                else if (Content.Length <= 1500)
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-Content");
                    SaveData($"{Context.Guild.Id}", $"JoinMessage-Content", $"{Base64Encode(Content)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Content set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Content can not exceed 1500 characters")); }
            }
        }

        [Command("SetFooter"), Alias("Footer")]
        public async Task SetFooter([Remainder] string Content)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (Content.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-Footer");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer removed"));
                }
                else if (Content.Length <= 1500)
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-Footer");
                    SaveData($"{Context.Guild.Id}", $"JoinMessage-Footer", $"{Base64Encode(Content)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Footer can not exceed 1500 characters")); }
            }
        }

        [Command("SetColour"), Alias("SetColor", "Colour", "Color")]
        public async Task SetColour(int R, int G, int B)
        {
            if (Permission(Context.User, Context.Channel))
            {
                Color Colour;
                try { Colour = new Color(R, G, B); }
                catch { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid colour", "[Pick colour](https://www.rapidtables.com/web/color/RGB_Color.html)\n[Support Discord](https://discord.gg/WsxqABZ)")); return; }

                DeleteData($"{Context.Guild.Id}", $"JoinMessage-Colour");
                SaveData($"{Context.Guild.Id}", $"JoinMessage-Colour", $"{R} {G} {B}");
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Colour set", "Preview this colour in this embed.").ToEmbedBuilder().WithColor(Colour).Build());
            }
        }

        [Command("SetIcon"), Alias("Icon", "SetAvatar", "Avatar")]
        public async Task SetIcon(string ImageURL)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (ImageURL.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-ImageURL");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Icon removed"));
                }
                else if (ImageURL.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-ImageURL");
                    SaveData($"{Context.Guild.Id}", $"JoinMessage-ImageURL", $"{Base64Encode(ImageURL)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Icon URL set", "Note that an invalid icon URL may result in the message not being sent at all"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Icon URL can not exceed 1000 characters")); }
            }
        }

        [Command("SetImage"), Alias("Image")]
        public async Task SetImage(string ImageURL)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (ImageURL.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-LargeImageURL");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Image removed"));
                }
                else if (ImageURL.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-LargeImageURL");
                    SaveData($"{Context.Guild.Id}", $"JoinMessage-LargeImageURL", $"{Base64Encode(ImageURL)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Image URL set", "Note that an invalid image URL may result in the message not being sent at all"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Image URL can not exceed 1000 characters")); }
            }
        }

        [Command("SetThumbnail"), Alias("Thumbnail")]
        public async Task SetThumbnail(string ImageURL)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (ImageURL.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-ThumbnailURL");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Thumbnail removed"));
                }
                else if (ImageURL.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-ThumbnailURL");
                    SaveData($"{Context.Guild.Id}", $"JoinMessage-ThumbnailURL", $"{Base64Encode(ImageURL)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Thumbnail URL set", "Note that an invalid thumbnail URL may result in the message not being sent at all"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Thumbnail URL can not exceed 1000 characters")); }
            }
        }

        [Command("SetFooterImage"), Alias("FooterImage")]
        public async Task SetFooterURL([Remainder] string ImageURL)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (ImageURL.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-FooterImageURL");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer image removed"));
                }
                else if (ImageURL.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-FooterImageURL");
                    SaveData($"{Context.Guild.Id}", $"JoinMessage-FooterImageURL", $"{Base64Encode(ImageURL)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer image set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Footer image URL can not exceed 1000 characters")); }
            }
        }

        [Command("SetNormalText"), Alias("NormalText")]
        public async Task SetNormalText([Remainder] string Content)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (Content.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-NormalText");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Normal text removed"));
                }
                else if (Content.Length <= 1500)
                {
                    DeleteData($"{Context.Guild.Id}", $"JoinMessage-NormalText");
                    SaveData($"{Context.Guild.Id}", $"JoinMessage-NormalText", $"{Base64Encode(Content)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Normal text set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Normal text can not exceed 1500 characters")); }
            }
        }

        [Command("Channel")]
        public async Task Channel(ITextChannel Channel)
        {
            if (Permission(Context.User as SocketGuildUser, Context.Channel))
            {
                if (BotHasPermissions(Channel, new ChannelPermission[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.EmbedLinks }, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "JoinMessage-Channel");
                    SaveData(Context.Guild.Id.ToString(), "JoinMessage-Channel", Channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Join message channel set", $"Join messages will now be sent in {Channel.Mention}"));
                }
            }
        }

        [Command("Channel")]
        public async Task Channel(string DM)
        {
            if (DM.ToLower() == "dm")
            {
                if (Permission(Context.User as SocketGuildUser, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "JoinMessage-Channel");
                    SaveData(Context.Guild.Id.ToString(), "JoinMessage-Channel", "DM");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Join message channel set", $"Join messages will now be sent to new users via DMs"));
                }
            }
            else
            {
                string Prefix = ".";
                try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {Prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
            }
        }

        [Command("Preview")]
        public async Task Preview()
        {
            var Message = await JoinMessage.GetJoinMessageAsync(Context.Guild, Context.User);
            await Context.Channel.SendMessageAsync(Message.Item1, embed: Message.Item2);
        }

        [Command("On"), Alias("Enable")]
        public async Task On()
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData($"{Context.Guild.Id}", $"JoinMessage-Enabled");
                SaveData($"{Context.Guild.Id}", $"JoinMessage-Enabled", $"true");
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Join message enabled", "Configure the join message using other commands in this category"));
            }
        }

        [Command("Off"), Alias("Disable")]
        public async Task Off(ITextChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData($"{Context.Guild.Id}", $"JoinMessage-Enabled");
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Join message disabled"));
            }
        }
    }
}