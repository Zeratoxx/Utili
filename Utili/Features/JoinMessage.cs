using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;

namespace Utili
{
    internal class JoinMessage
    {
        public async Task JoinMessage_UserJoined(SocketGuildUser user)
        {
            if (DataExists(user.Guild.Id.ToString(), "JoinMessage-Enabled", "true"))
            {
                (string, Embed) joinMessage = await GetJoinMessageAsync(user.Guild, user);

                string channelData;
                try { channelData = GetFirstData(user.Guild.Id.ToString(), "JoinMessage-Channel").Value; } catch { channelData = "DM"; }
                if (ulong.TryParse(channelData, out ulong channelId))
                {
                    ITextChannel channel = user.Guild.GetTextChannel(channelId);
                    await channel.SendMessageAsync(joinMessage.Item1, embed: joinMessage.Item2);
                }
                else
                {
                    IDMChannel channel = await user.GetOrCreateDMChannelAsync();
                    await channel.SendMessageAsync(joinMessage.Item1, embed: joinMessage.Item2);
                }
            }
        }

        public static async Task<(string, Embed)> GetJoinMessageAsync(SocketGuild guild, SocketUser user)
        {
            string title;
            try { title = GetFirstData($"{guild.Id}", "JoinMessage-Title").Value; }
            catch { title = ""; }
            try { title = Base64Decode(title).Replace("%user%", $"{user.Username}#{user.Discriminator}"); } catch { }

            string content;
            try { content = GetFirstData($"{guild.Id}", "JoinMessage-Content").Value; }
            catch { content = ""; }
            try { content = Base64Decode(content).Replace("%user%", $"{user.Mention}"); } catch { }

            string normalText;
            try { normalText = GetFirstData($"{guild.Id}", "JoinMessage-NormalText").Value; }
            catch { normalText = ""; }
            try { normalText = Base64Decode(normalText).Replace("%user%", $"{user.Mention}"); } catch { }

            string footer;
            try { footer = GetFirstData($"{guild.Id}", "JoinMessage-Footer").Value; }
            catch { footer = ""; }
            try { footer = Base64Decode(footer).Replace("%user%", $"{user.Username}#{user.Discriminator}"); } catch { }

            string imageUrl;
            try { imageUrl = GetFirstData($"{guild.Id}", "JoinMessage-ImageURL").Value; }
            catch { imageUrl = ""; }
            try { imageUrl = Base64Decode(imageUrl); } catch { }
            if (imageUrl == "user") imageUrl = user.GetAvatarUrl();

            string thumbnailUrl;
            try { thumbnailUrl = GetFirstData($"{guild.Id}", "JoinMessage-ThumbnailURL").Value; }
            catch { thumbnailUrl = ""; }
            try { thumbnailUrl = Base64Decode(thumbnailUrl); } catch { }
            if (thumbnailUrl == "user") thumbnailUrl = user.GetAvatarUrl();

            string largeImageUrl;
            try { largeImageUrl = GetFirstData($"{guild.Id}", "JoinMessage-LargeImageURL").Value; }
            catch { largeImageUrl = ""; }
            try { largeImageUrl = Base64Decode(largeImageUrl); } catch { }
            if (largeImageUrl == "user") largeImageUrl = user.GetAvatarUrl();

            string footerImageUrl;
            try { footerImageUrl = GetFirstData($"{guild.Id}", "JoinMessage-FooterImageURL").Value; }
            catch { footerImageUrl = ""; }
            try { footerImageUrl = Base64Decode(footerImageUrl); } catch { }
            if (footerImageUrl == "user") footerImageUrl = user.GetAvatarUrl();

            string colourString;
            try { colourString = GetFirstData($"{guild.Id}", "JoinMessage-Colour").Value; }
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

            return (normalText, embed.Build());
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
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Join Message", HelpContent, $"Prefix these commands with {prefix}joinmessage"));
        }

        [Command("")]
        public async Task Empty()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Join Message", HelpContent, $"Prefix these commands with {prefix}joinmessage"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Join Message", "This feature allows you to send a message in a channel or directly to users when they join the guild"));
        }

        [Command("SetTitle"), Alias("Title")]
        public async Task SetTitle([Remainder] string content)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (content.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", "JoinMessage-Title");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Title removed"));
                }
                else if (content.Length <= 500)
                {
                    DeleteData($"{Context.Guild.Id}", "JoinMessage-Title");
                    SaveData($"{Context.Guild.Id}", "JoinMessage-Title", $"{Base64Encode(content)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Title set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Title can not exceed 500 characters")); }
            }
        }

        [Command("SetContent"), Alias("Content")]
        public async Task SetContent([Remainder] string content)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (content.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", "JoinMessage-Content");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Content removed"));
                }
                else if (content.Length <= 1500)
                {
                    DeleteData($"{Context.Guild.Id}", "JoinMessage-Content");
                    SaveData($"{Context.Guild.Id}", "JoinMessage-Content", $"{Base64Encode(content)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Content set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Content can not exceed 1500 characters")); }
            }
        }

        [Command("SetFooter"), Alias("Footer")]
        public async Task SetFooter([Remainder] string content)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (content.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", "JoinMessage-Footer");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer removed"));
                }
                else if (content.Length <= 1500)
                {
                    DeleteData($"{Context.Guild.Id}", "JoinMessage-Footer");
                    SaveData($"{Context.Guild.Id}", "JoinMessage-Footer", $"{Base64Encode(content)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Footer can not exceed 1500 characters")); }
            }
        }

        [Command("SetColour"), Alias("SetColor", "Colour", "Color")]
        public async Task SetColour(int r, int g, int b)
        {
            if (Permission(Context.User, Context.Channel))
            {
                Color colour;
                try { colour = new Color(r, g, b); }
                catch { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid colour", "[Pick colour](https://www.rapidtables.com/web/color/RGB_Color.html)\n[Support Discord](https://discord.gg/WsxqABZ)")); return; }

                DeleteData($"{Context.Guild.Id}", "JoinMessage-Colour");
                SaveData($"{Context.Guild.Id}", "JoinMessage-Colour", $"{r} {g} {b}");
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Colour set", "Preview this colour in this embed.").ToEmbedBuilder().WithColor(colour).Build());
            }
        }

        [Command("SetIcon"), Alias("Icon", "SetAvatar", "Avatar")]
        public async Task SetIcon(string imageUrl)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (imageUrl.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", "JoinMessage-ImageURL");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Icon removed"));
                }
                else if (imageUrl.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", "JoinMessage-ImageURL");
                    SaveData($"{Context.Guild.Id}", "JoinMessage-ImageURL", $"{Base64Encode(imageUrl)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Icon URL set", "Note that an invalid icon URL may result in the message not being sent at all"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Icon URL can not exceed 1000 characters")); }
            }
        }

        [Command("SetImage"), Alias("Image")]
        public async Task SetImage(string imageUrl)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (imageUrl.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", "JoinMessage-LargeImageURL");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Image removed"));
                }
                else if (imageUrl.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", "JoinMessage-LargeImageURL");
                    SaveData($"{Context.Guild.Id}", "JoinMessage-LargeImageURL", $"{Base64Encode(imageUrl)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Image URL set", "Note that an invalid image URL may result in the message not being sent at all"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Image URL can not exceed 1000 characters")); }
            }
        }

        [Command("SetThumbnail"), Alias("Thumbnail")]
        public async Task SetThumbnail(string imageUrl)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (imageUrl.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", "JoinMessage-ThumbnailURL");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Thumbnail removed"));
                }
                else if (imageUrl.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", "JoinMessage-ThumbnailURL");
                    SaveData($"{Context.Guild.Id}", "JoinMessage-ThumbnailURL", $"{Base64Encode(imageUrl)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Thumbnail URL set", "Note that an invalid thumbnail URL may result in the message not being sent at all"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Thumbnail URL can not exceed 1000 characters")); }
            }
        }

        [Command("SetFooterImage"), Alias("FooterImage")]
        public async Task SetFooterUrl([Remainder] string imageUrl)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (imageUrl.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", "JoinMessage-FooterImageURL");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer image removed"));
                }
                else if (imageUrl.Length <= 1000)
                {
                    DeleteData($"{Context.Guild.Id}", "JoinMessage-FooterImageURL");
                    SaveData($"{Context.Guild.Id}", "JoinMessage-FooterImageURL", $"{Base64Encode(imageUrl)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Footer image set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Footer image URL can not exceed 1000 characters")); }
            }
        }

        [Command("SetNormalText"), Alias("NormalText")]
        public async Task SetNormalText([Remainder] string content)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (content.ToLower() == "none")
                {
                    DeleteData($"{Context.Guild.Id}", "JoinMessage-NormalText");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Normal text removed"));
                }
                else if (content.Length <= 1500)
                {
                    DeleteData($"{Context.Guild.Id}", "JoinMessage-NormalText");
                    SaveData($"{Context.Guild.Id}", "JoinMessage-NormalText", $"{Base64Encode(content)}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Normal text set"));
                }
                else { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Normal text can not exceed 1500 characters")); }
            }
        }

        [Command("Channel")]
        public async Task Channel(ITextChannel channel)
        {
            if (Permission(Context.User as SocketGuildUser, Context.Channel))
            {
                if (BotHasPermissions(channel, new[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.EmbedLinks }, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "JoinMessage-Channel");
                    SaveData(Context.Guild.Id.ToString(), "JoinMessage-Channel", channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Join message channel set", $"Join messages will now be sent in {channel.Mention}"));
                }
            }
        }

        [Command("Channel")]
        public async Task Channel(string dm)
        {
            if (dm.ToLower() == "dm")
            {
                if (Permission(Context.User as SocketGuildUser, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "JoinMessage-Channel");
                    SaveData(Context.Guild.Id.ToString(), "JoinMessage-Channel", "DM");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Join message channel set", "Join messages will now be sent to new users via DMs"));
                }
            }
            else
            {
                string prefix = ".";
                try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
            }
        }

        [Command("Preview")]
        public async Task Preview()
        {
            (string, Embed) message = await JoinMessage.GetJoinMessageAsync(Context.Guild, Context.User);
            await Context.Channel.SendMessageAsync(message.Item1, embed: message.Item2);
        }

        [Command("On"), Alias("Enable")]
        public async Task On()
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData($"{Context.Guild.Id}", "JoinMessage-Enabled");
                SaveData($"{Context.Guild.Id}", "JoinMessage-Enabled", "true");
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Join message enabled", "Configure the join message using other commands in this category"));
            }
        }

        [Command("Off"), Alias("Disable")]
        public async Task Off()
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData($"{Context.Guild.Id}", "JoinMessage-Enabled");
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Join message disabled"));
            }
        }
    }
}