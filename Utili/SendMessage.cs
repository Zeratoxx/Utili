using Discord;

namespace Utili
{
    internal class SendMessage
    {
        public static Embed GetEmbed(string marking, string shortMessage, string longMessage = null)
        {
            EmbedBuilder embed = new EmbedBuilder();

            EmbedAuthorBuilder embedAuthor = new EmbedAuthorBuilder();

            switch (marking)
            {
                case "Yes":
                    embedAuthor.IconUrl = "https://cdn.discordapp.com/attachments/591310067979255808/670237090214182913/Check.png";
                    embed.WithColor(67, 181, 129);
                    break;

                case "No":
                    embedAuthor.IconUrl = "https://media.discordapp.net/attachments/591310067979255808/670237599218008078/Cross.png?width=678&height=678";
                    embed.WithColor(181, 67, 67);
                    break;

                case "Neutral":
                    embedAuthor.IconUrl = "https://cdn.discordapp.com/attachments/591310067979255808/670237899656265748/Line.png";
                    embed.WithColor(195, 195, 195);
                    break;

                default:
                    embed.WithColor(195, 195, 195);
                    break;
            }

            embedAuthor.WithName(shortMessage);

            embed.WithAuthor(embedAuthor);

            if (longMessage != null) embed.WithDescription(longMessage);

            return embed.Build();
        }

        public static Embed GetLargeEmbed(string title, string content, string footer = null, string marking = null, string imageUrl = null)
        {
            EmbedBuilder embed = new EmbedBuilder();

            EmbedAuthorBuilder embedAuthor = new EmbedAuthorBuilder();
            embedAuthor.WithName(title);

            switch (marking)
            {
                case "Yes":
                    embedAuthor.IconUrl = "https://cdn.discordapp.com/attachments/591310067979255808/670237090214182913/Check.png";
                    embed.WithColor(67, 181, 129);
                    break;

                case "No":
                    embedAuthor.IconUrl = "https://media.discordapp.net/attachments/591310067979255808/670237599218008078/Cross.png?width=678&height=678";
                    embed.WithColor(181, 67, 67);
                    break;

                case "Neutral":
                    embedAuthor.IconUrl = "https://cdn.discordapp.com/attachments/591310067979255808/670237899656265748/Line.png";
                    embed.WithColor(195, 195, 195);
                    break;

                default:
                    embed.WithColor(67, 181, 129);
                    break;
            }

            if (imageUrl != null) embedAuthor.IconUrl = imageUrl;

            embed.WithAuthor(embedAuthor);

            embed.WithDescription(content);

            if (footer != null) embed.WithFooter(footer);

            return embed.Build();
        }
    }
}