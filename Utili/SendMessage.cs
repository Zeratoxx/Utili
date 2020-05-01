using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using static Utili.Json;

namespace Utili
{
    class SendMessage
    {
        public static Embed GetEmbed(string Marking, string ShortMessage, string LongMessage = null)
        {
            EmbedBuilder Embed = new EmbedBuilder();

            EmbedAuthorBuilder EmbedAuthor = new EmbedAuthorBuilder();

            switch (Marking)
            {
                case "Yes":
                    EmbedAuthor.IconUrl = "https://cdn.discordapp.com/attachments/591310067979255808/670237090214182913/Check.png";
                    Embed.WithColor(67, 181, 129);
                    break;
                case "No":
                    EmbedAuthor.IconUrl = "https://media.discordapp.net/attachments/591310067979255808/670237599218008078/Cross.png?width=678&height=678";
                    Embed.WithColor(181, 67, 67);
                    break;
                case "Neutral":
                    EmbedAuthor.IconUrl = "https://cdn.discordapp.com/attachments/591310067979255808/670237899656265748/Line.png";
                    Embed.WithColor(195, 195, 195);
                    break;
                default:
                    Embed.WithColor(195, 195, 195);
                    break;
            }

            EmbedAuthor.WithName(ShortMessage);

            Embed.WithAuthor(EmbedAuthor);

            if (LongMessage != null) Embed.WithDescription(LongMessage);

            return Embed.Build();
        }

        public static Embed GetLargeEmbed(string Title, string Content, string Footer = null, string Marking = null, string ImageURL = null)
        {
            EmbedBuilder Embed = new EmbedBuilder();

            EmbedAuthorBuilder EmbedAuthor = new EmbedAuthorBuilder();
            EmbedAuthor.WithName(Title);

            switch (Marking)
            {
                case "Yes":
                    EmbedAuthor.IconUrl = "https://cdn.discordapp.com/attachments/591310067979255808/670237090214182913/Check.png";
                    Embed.WithColor(67, 181, 129);
                    break;
                case "No":
                    EmbedAuthor.IconUrl = "https://media.discordapp.net/attachments/591310067979255808/670237599218008078/Cross.png?width=678&height=678";
                    Embed.WithColor(181, 67, 67);
                    break;
                case "Neutral":
                    EmbedAuthor.IconUrl = "https://cdn.discordapp.com/attachments/591310067979255808/670237899656265748/Line.png";
                    Embed.WithColor(195, 195, 195);
                    break;
                default:
                    Embed.WithColor(67, 181, 129);
                    break;
            }

            if (ImageURL != null) EmbedAuthor.IconUrl = ImageURL;

            Embed.WithAuthor(EmbedAuthor);

            Embed.WithDescription(Content);

            if (Footer != null) Embed.WithFooter(Footer);

            return Embed.Build();
        }
    }
}
