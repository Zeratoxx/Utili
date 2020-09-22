using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Discord.WebSocket;
using static Utili.Data;
using static Utili.SendMessage;

namespace Utili
{
    internal class Logic
    {
        public static bool Permission(SocketUser user, ISocketMessageChannel channel, bool crossGuild = false)
        {
            SocketGuildUser guildUser = user as SocketGuildUser;
            if (guildUser.GuildPermissions.ManageGuild) return true;
            if (!crossGuild) channel.SendMessageAsync(embed: GetEmbed("No", "Permission denied", "You need the `ManageGuild` permission to use that command"));
            else channel.SendMessageAsync(embed: GetEmbed("No", "Permission denied", "You need the `ManageGuild` permission in that guild to use that command"));
            return false;
        }

        public static bool MessagePermission(SocketUser user, ISocketMessageChannel channel, ISocketMessageChannel sendChannel)
        {
            SocketGuildUser guildUser = user as SocketGuildUser;
            if (guildUser.GetPermissions(channel as SocketGuildChannel).ManageMessages) return true;
            sendChannel.SendMessageAsync(embed: GetEmbed("No", "Permission denied", "You need the `ManageMessages` permission to use that command"));
            return false;
        }

        public static bool AdminPermission(SocketUser user, ISocketMessageChannel channel)
        {
            SocketGuildUser guildUser = user as SocketGuildUser;
            if (guildUser.GuildPermissions.Administrator) return true;
            channel.SendMessageAsync(embed: GetEmbed("No", "Permission denied", "You need the `Administrator` permission to use that command"));
            return false;
        }

        public static bool OwnerPermission(SocketUser user, ISocketMessageChannel channel)
        {
            SocketGuildUser guildUser = user as SocketGuildUser;
            if (guildUser.Id == 218613903653863427) return true;
            try { channel.SendMessageAsync(embed: GetEmbed("No", "Permission denied", "That command is only for the bot owner")); }
            catch { }
            return false;
        }

        public static string DisplayTimespan(TimeSpan time, bool @short = false)
        {
            if (time == TimeSpan.FromSeconds(0)) return "0 seconds";

            string @string = "";

            if (!@short)
            {
                if (time.Days > 1) @string += $"{time.Days} days, ";
                else if (time.Days == 1) @string += "1 day, ";

                if (time.Hours > 1) @string += $"{time.Hours} hours, ";
                else if (time.Hours == 1) @string += "1 hour, ";

                if (time.Minutes > 1) @string += $"{time.Minutes} minutes, ";
                else if (time.Minutes == 1) @string += "1 minute, ";

                if (time.Seconds > 1) @string += $"{time.Seconds} seconds, ";
                else if (time.Seconds == 1) @string += "1 second, ";

                if (time.Milliseconds > 1) @string += $"{time.Milliseconds} milliseconds, ";
                else if (time.Milliseconds == 1) @string += "1 millisecond, ";
            }
            else
            {
                if (time.Days > 0) @string += $"{time.Days}d, ";
                if (time.Hours > 0) @string += $"{time.Hours}h, ";
                if (time.Minutes > 0) @string += $"{time.Minutes}m, ";
            }

            return @string.Remove(@string.Length - 2);
        }

        public static Embed GuildInfo(SocketGuild guild)
        {
            EmbedBuilder embed = GetLargeEmbed(guild.Name, $"ID {guild.Id}", imageUrl: guild.IconUrl).ToEmbedBuilder();

            embed.AddField("Owner", $"{guild.Owner}", true);

            int members = guild.Users.Where(x => x.IsBot == false).Count();
            int bots = guild.Users.Where(x => x.IsBot).Count();
            embed.AddField("Humans", members, true);
            embed.AddField("Bots", bots, true);

            embed.AddField("Age", $"{DisplayTimespan(DateTime.Now - guild.CreatedAt, true)}", true);

            embed.AddField("Database entries", $"{GetData(guild.Id.ToString()).Count()}", true);

            embed.AddField("Channels", $"{guild.Channels.Count}", true);

            embed.AddField("Roles", guild.Roles.Where(x => x.IsManaged == false).Count(), true);

            embed.AddField("Custom Emotes", $"{guild.Emotes.Count}", true);

            return embed.Build();
        }

        public static bool BotHasPermissions(ITextChannel channel, ChannelPermission[] permissions, ISocketMessageChannel contextChannel, bool send = true)
        {
            SocketGuildUser user = (channel as SocketGuildChannel).Guild.GetUser(Program.Client.CurrentUser.Id);

            bool hasPermissions = true;
            List<string> errors = new List<string>();
            foreach (ChannelPermission permission in permissions)
            {
                if (!user.GetPermissions(channel).Has(permission))
                {
                    hasPermissions = false;
                    errors.Add($"`{permission}`");
                }
            }

            if (hasPermissions) return true;
            if (send)
            {
                string content = "";
                int i = 0;
                foreach (string error in errors)
                {
                    if (i == errors.Count - 1) content += error; //If last one
                    else if (i == errors.Count - 2) content += $"{error} and the "; //If second last one
                    else content += $"{error}, the "; //Any other one

                    i++;
                }

                contextChannel.SendMessageAsync(embed: GetEmbed("No", "I don't have permission", $"I need the {content} permission to do that\n[Support Discord](https://discord.gg/WsxqABZ)"));
                return false;
            }

            return false;
        }

        public static bool BotHasPermissions(SocketGuild guild, GuildPermission[] permissions, ISocketMessageChannel contextChannel, bool send = true)
        {
            SocketGuildUser user = guild.GetUser(Program.Client.CurrentUser.Id);

            bool hasPermissions = true;
            List<string> errors = new List<string>();
            foreach (GuildPermission permission in permissions)
            {
                if (!user.GuildPermissions.Has(permission))
                {
                    hasPermissions = false;
                    errors.Add($"`{permission}`");
                }
            }

            if (hasPermissions) return true;
            if (send)
            {
                string content = "";
                int i = 0;
                foreach (string error in errors)
                {
                    if (i == errors.Count - 1) content += error; //If last one
                    else if (i == errors.Count - 2) content += $"{error} and the "; //If second last one
                    else content += $"{error}, the "; //Any other one

                    i++;
                }

                contextChannel.SendMessageAsync(embed: GetEmbed("No", "I don't have permission", $"I need the {content} guild permission to do that\n[Support Discord](https://discord.gg/WsxqABZ)"));
                return false;
            }

            return false;
        }

        public static Emote GetGuildEmote(string input, SocketGuild guild)
        {
            try { return guild.Emotes.First(x => x.Name == input); } catch { }
            try { return guild.Emotes.First(x => x.Name == input.Split(":").ToArray()[1]); } catch { }

            return null;
        }

        public static Emoji GetDiscordEmote(string input)
        {
            try { return new Emoji(input); } catch { }

            return null;
        }

        public static int GetMaxWorkers()
        {
            int amount = (int)Math.Round(Program.Client.Guilds.Count / 40d);
            if (amount == 0) return 1;
            return amount;
        }

        public static string Base64Encode(string plainText)
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            byte[] base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}