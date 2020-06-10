using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Utili.SendMessage;
using static Utili.Json;
using static Utili.Data;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Utili
{
    class Logic
    {
        public static bool Permission(SocketUser User, ISocketMessageChannel Channel, bool CrossGuild = false)
        {
            SocketGuildUser user = User as SocketGuildUser;
            if (user.GuildPermissions.ManageGuild == true) return true;
            else
            {
                if(!CrossGuild) Channel.SendMessageAsync(embed: GetEmbed("No", "Permission denied", "You need the `ManageGuild` permission to use that command"));
                else Channel.SendMessageAsync(embed: GetEmbed("No", "Permission denied", "You need the `ManageGuild` permission in that guild to use that command"));
                return false;
            }
        }

        public static bool MessagePermission(SocketUser User, ISocketMessageChannel Channel, ISocketMessageChannel SendChannel)
        {
            SocketGuildUser user = User as SocketGuildUser;
            if (user.GetPermissions(Channel as SocketGuildChannel).ManageMessages == true) return true;
            else
            {
                SendChannel.SendMessageAsync(embed: GetEmbed("No", "Permission denied", "You need the `ManageMessages` permission to use that command"));
                return false;
            }
        }

        public static bool AdminPermission(SocketUser User, ISocketMessageChannel Channel)
        {
            SocketGuildUser user = User as SocketGuildUser;
            if (user.GuildPermissions.Administrator == true) return true;
            else
            {
                Channel.SendMessageAsync(embed: GetEmbed("No", "Permission denied", "You need the `Administrator` permission to use that command"));
                return false;
            }
        }

        public static bool OwnerPermission(SocketUser User, ISocketMessageChannel Channel)
        {
            SocketGuildUser user = User as SocketGuildUser;
            if (user.Id == 218613903653863427) return true;
            else
            {
                Channel.SendMessageAsync(embed: GetEmbed("No", "Permission denied", "That command is only for the bot owner"));
                return false;
            }
        }

        public static string DisplayTimespan(TimeSpan Time, bool Short = false)
        {
            if (Time == TimeSpan.FromSeconds(0)) return "0 seconds";

            string String = "";

            if (!Short)
            {
                if (Time.Days > 1) String += $"{Time.Days} days, ";
                else if (Time.Days == 1) String += $"1 day, ";

                if (Time.Hours > 1) String += $"{Time.Hours} hours, ";
                else if (Time.Hours == 1) String += $"1 hour, ";

                if (Time.Minutes > 1) String += $"{Time.Minutes} minutes, ";
                else if (Time.Minutes == 1) String += $"1 minute, ";

                if (Time.Seconds > 1) String += $"{Time.Seconds} seconds, ";
                else if (Time.Seconds == 1) String += $"1 second, ";

                if (Time.Milliseconds > 1) String += $"{Time.Milliseconds} milliseconds, ";
                else if (Time.Milliseconds == 1) String += $"1 millisecond, ";
            }
            else
            {
                if (Time.Days > 0) String += $"{Time.Days}d, ";
                if (Time.Hours > 0) String += $"{Time.Hours}h, ";
                if (Time.Minutes > 0) String += $"{Time.Minutes}m, ";
            }
            
            return String.Remove(String.Length - 2);
        }

        public static Embed GuildInfo(SocketGuild Guild)
        {
            EmbedBuilder Embed = GetLargeEmbed(Guild.Name, $"ID {Guild.Id}", ImageURL: Guild.IconUrl).ToEmbedBuilder();

            Embed.AddField("Owner", $"{Guild.Owner}", true);

            int Members = Guild.Users.Where(x => x.IsBot == false).Count();
            int Bots = Guild.Users.Where(x => x.IsBot == true).Count();
            Embed.AddField("Humans", Members, true);
            Embed.AddField("Bots", Bots, true);

            Embed.AddField("Age", $"{Logic.DisplayTimespan(DateTime.Now - Guild.CreatedAt, true)}", true);

            Embed.AddField("Database entries", $"{Data.GetData(Guild.Id.ToString()).Count()}", true);

            Embed.AddField($"Channels", $"{Guild.Channels.Count}", true);

            Embed.AddField($"Roles", Guild.Roles.Where(x => x.IsManaged == false).Count(), true);

            Embed.AddField("Custom Emotes", $"{Guild.Emotes.Count}", true);

            return Embed.Build();
        }

        public static bool BotHasPermissions(ITextChannel Channel, ChannelPermission[] Permissions, ISocketMessageChannel ContextChannel, bool Send = true)
        {
            SocketGuildUser User = (Channel as SocketGuildChannel).Guild.GetUser(Program.Client.CurrentUser.Id);

            bool HasPermissions = true;
            List<string> Errors = new List<string>();
            foreach(ChannelPermission Permission in Permissions)
            {
                if (!User.GetPermissions(Channel as IGuildChannel).Has(Permission))
                {
                    HasPermissions = false;
                    Errors.Add($"`{Permission.ToString()}`");
                }
            }

            if (HasPermissions) return true;
            else
            {
                if (Send)
                {
                    string Content = "";
                    int i = 0;
                    foreach (string Error in Errors)
                    {
                        if (i == Errors.Count - 1) Content += Error; //If last one
                        else if (i == Errors.Count - 2) Content += $"{Error} and the "; //If second last one
                        else Content += $"{Error}, the "; //Any other one

                        i++;
                    }

                    ContextChannel.SendMessageAsync(embed: GetEmbed("No", "I don't have permission", $"I need the {Content} permission to do that\n[Support Discord](https://discord.gg/WsxqABZ)"));
                    return false;
                }
                else return false;
            }
        }

        public static bool BotHasPermissions(SocketGuild Guild, GuildPermission[] Permissions, ISocketMessageChannel ContextChannel, bool Send = true)
        {
            SocketGuildUser User = Guild.GetUser(Program.Client.CurrentUser.Id);

            bool HasPermissions = true;
            List<string> Errors = new List<string>();
            foreach (GuildPermission Permission in Permissions)
            {
                if(!User.GuildPermissions.Has(Permission))
                {
                    HasPermissions = false;
                    Errors.Add($"`{Permission}`");
                }
            }

            if (HasPermissions) return true;
            else
            {
                if (Send)
                {
                    string Content = "";
                    int i = 0;
                    foreach (string Error in Errors)
                    {
                        if (i == Errors.Count - 1) Content += Error; //If last one
                        else if (i == Errors.Count - 2) Content += $"{Error} and the "; //If second last one
                        else Content += $"{Error}, the "; //Any other one

                        i++;
                    }

                    ContextChannel.SendMessageAsync(embed: GetEmbed("No", "I don't have permission", $"I need the {Content} guild permission to do that\n[Support Discord](https://discord.gg/WsxqABZ)"));
                    return false;
                }
                else return false;
            }
        }

        public static Emote GetGuildEmote(string Input, SocketGuild Guild)
        {
            try { return Guild.Emotes.First(x => x.Name == Input); } catch { };
            try { return Guild.Emotes.First(x => x.Name == Input.Split(":").ToArray()[1]); } catch { };

            return null;
        }

        public static Emoji GetDiscordEmote(string Input)
        {
            try { return new Emoji(Input); } catch { };

            return null;
        }

        public static int GetMaxWorkers()
        {
            if (DBLatency < 500) return 5;
            else if (DBLatency < 1000) return 3;
            else if (DBLatency < 2000) return 2;
            else if (DBLatency < 5000) return 1;
            else return 0;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}