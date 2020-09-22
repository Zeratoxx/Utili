using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;

namespace Utili
{
    internal class MessageLogs
    {
        public async Task MessageLogs_MessageReceived(SocketMessage messageParam)
        {
            SocketUserMessage message = messageParam as SocketUserMessage;
            SocketCommandContext context = new SocketCommandContext(Program._client, message);

            if (context.User.IsBot || context.User.IsWebhook) return;

            Random random = new Random();

            if (DataExists(context.Guild.Id.ToString(), "MessageLogs-Channel", context.Channel.Id.ToString()))
            {
                await SaveMessageAsync(context);

                // Every so often, delete message log channel entries for non-existant channels
                if (random.Next(0, 10) == 5)
                {
                    List<Data> channelData = GetData(context.Guild.Id.ToString(), "MessageLogs-Channel");
                    foreach (Data channel in channelData)
                    {
                        if (!context.Guild.TextChannels.Select(x => x.Id).Contains(ulong.Parse(channel.Value)) && context.Guild.TextChannels.Count > 0)
                        {
                            DeleteData(context.Guild.Id.ToString(), "MessageLogs-Channel", channel.Value);
                        }
                    }
                }
            }

            // Every so often, delete logs older than 30 days to comply with Discord's rules
            if (random.Next(0, 25) == 10) RunNonQuery($"DELETE FROM Utili_MessageLogs WHERE(Timestamp < '{ToSqlTime(DateTime.Now - TimeSpan.FromDays(30))}')");
        }

        public async Task MessageLogs_MessageDeleted(Cacheable<IMessage, ulong> partialMessage, ISocketMessageChannel channel)
        {
            SocketGuild guild = (channel as SocketTextChannel).Guild;
            if (DataExists(guild.Id.ToString(), "MessageLogs-Channel", channel.Id.ToString()))
            {
                MessageData message = await GetMessageAsync(partialMessage.Id);
                string content = Decrypt(message.EncryptedContent, ulong.Parse(message.GuildId), ulong.Parse(message.ChannelId));

                if (message.ChannelId == channel.Id.ToString())
                {
                    RunNonQuery($"DELETE FROM Utili_MessageLogs WHERE ID = {message.Id}");
                    SocketTextChannel logChannel = guild.GetTextChannel(ulong.Parse(GetFirstData(guild.Id.ToString(), "MessageLogs-LogChannel").Value));
                    SocketGuildUser user = guild.GetUser(ulong.Parse(message.UserId));

                    EmbedBuilder embed = new EmbedBuilder();
                    embed.WithColor(245, 66, 66);
                    embed.WithCurrentTimestamp();
                    embed.WithDescription($"**Message sent by {user.Mention} deleted in {(channel as SocketTextChannel).Mention}**\n{content}");

                    EmbedAuthorBuilder author = new EmbedAuthorBuilder
                    {
                        Name = $"{user.Username}#{user.Discriminator}",
                        IconUrl = user.GetAvatarUrl()
                    };
                    embed.WithAuthor(author);
                    embed.WithFooter($"Message: {partialMessage.Id} | User: {user.Id}");

                    await logChannel.SendMessageAsync(embed: embed.Build());
                }
            }
        }

        public async Task MessageLogs_MessageEdited(Cacheable<IMessage, ulong> partialMessage, SocketMessage newMessage, ISocketMessageChannel channel)
        {
            SocketGuild guild = (channel as SocketTextChannel).Guild;
            SocketCommandContext context = new SocketCommandContext(Program._client, newMessage as SocketUserMessage);

            if (DataExists(guild.Id.ToString(), "MessageLogs-Channel", channel.Id.ToString()))
            {
                MessageData message = await GetMessageAsync(partialMessage.Id);
                string content = Decrypt(message.EncryptedContent, ulong.Parse(message.GuildId), ulong.Parse(message.ChannelId));

                if (DataExists(message.GuildId, "MessageLogs-Channel", message.ChannelId))
                {
                    if (newMessage.Content == content) return;

                    RunNonQuery($"DELETE FROM Utili_MessageLogs WHERE ID = {message.Id}");
                    SocketTextChannel logChannel = guild.GetTextChannel(ulong.Parse(GetFirstData(guild.Id.ToString(), "MessageLogs-LogChannel").Value));
                    SocketGuildUser user = guild.GetUser(ulong.Parse(message.UserId));

                    EmbedBuilder embed = new EmbedBuilder();
                    embed.WithColor(66, 182, 245);
                    embed.WithCurrentTimestamp();
                    embed.WithDescription($"**Message sent by {user.Mention} edited in {(channel as SocketTextChannel).Mention}**  [Jump]({newMessage.GetJumpUrl()})");
                    embed.AddField("Before", content);
                    embed.AddField("After", newMessage.Content);
                    EmbedAuthorBuilder author = new EmbedAuthorBuilder
                    {
                        Name = $"{user.Username}#{user.Discriminator}",
                        IconUrl = user.GetAvatarUrl()
                    };
                    embed.WithAuthor(author);
                    embed.WithFooter($"Message: {partialMessage.Id} | User: {user.Id}");

                    await SaveMessageAsync(context);

                    await logChannel.SendMessageAsync(embed: embed.Build());
                }
            }
        }

        public async Task MessageLogs_ChannelCreated(SocketChannel channelParam)
        {
            SocketTextChannel channel = channelParam as SocketTextChannel;
            if (DataExists(channel.Guild.Id.ToString(), "MessageLogs-DefaultOn"))
            {
                SaveData(channel.Guild.Id.ToString(), "MessageLogs-Channel", channel.Id.ToString());
            }
        }

        public static string Encrypt(string textData, ulong guildId, ulong channelId)
        {
            string encryptionKey = $"{guildId * channelId}-{channelId * 3 - guildId}";

            RijndaelManaged objrij = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                KeySize = 0x80,
                BlockSize = 0x80
            };
            byte[] passBytes = Encoding.UTF8.GetBytes(encryptionKey);
            byte[] encryptionkeyBytes = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            int len = passBytes.Length;
            if (len > encryptionkeyBytes.Length)
            {
                len = encryptionkeyBytes.Length;
            }
            Array.Copy(passBytes, encryptionkeyBytes, len);
            objrij.Key = encryptionkeyBytes;
            objrij.IV = encryptionkeyBytes;
            ICryptoTransform objtransform = objrij.CreateEncryptor();
            byte[] textDataByte = Encoding.UTF8.GetBytes(textData);
            return Convert.ToBase64String(objtransform.TransformFinalBlock(textDataByte, 0, textDataByte.Length));
        }

        public static string Decrypt(string encryptedText, ulong guildId, ulong channelId)
        {
            string encryptionKey = $"{guildId * channelId}-{channelId * 3 - guildId}";

            RijndaelManaged objrij = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                KeySize = 0x80,
                BlockSize = 0x80
            };
            byte[] encryptedTextByte = Convert.FromBase64String(encryptedText);
            byte[] passBytes = Encoding.UTF8.GetBytes(encryptionKey);
            byte[] encryptionkeyBytes = new byte[0x10];
            int len = passBytes.Length;
            if (len > encryptionkeyBytes.Length)
            {
                len = encryptionkeyBytes.Length;
            }
            Array.Copy(passBytes, encryptionkeyBytes, len);
            objrij.Key = encryptionkeyBytes;
            objrij.IV = encryptionkeyBytes;
            byte[] textByte = objrij.CreateDecryptor().TransformFinalBlock(encryptedTextByte, 0, encryptedTextByte.Length);
            return Encoding.UTF8.GetString(textByte);
        }
    }

    [Group("MessageLogs"), Alias("Logs")]
    public class MessageLogsCommands : ModuleBase<SocketCommandContext>
    {
        public static string HelpContent =
                "help - Show this list\n" +
                "about - Display feature information\n" +
                "channel [channel] - Set the log channel\n" +
                "on [channel | all] - Enable logging in a channel\n" +
                "off [channel | all] - Disable logging in a channel";

        [Command("Help")]
        public async Task Help()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Message Logs", HelpContent, $"Prefix these commands with {prefix}logs"));
        }

        [Command("")]
        public async Task Empty()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Message Logs", HelpContent, $"Prefix these commands with {prefix}logs"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Message Logs", "Automatically posts messages in a log channel when they're deleted or edited. You can choose which channels it's turned on in, it's disabled by default.\nThe message content is always securely encrypted and is removed from the database once the message is deleted or edited.\nMessages are stored for up to 30 days."));
        }

        [Command("Channel")]
        public async Task LogChannel(ITextChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(channel, new[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages }, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "MessageLogs-LogChannel");
                    SaveData(Context.Guild.Id.ToString(), "MessageLogs-LogChannel", channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set message logs channel", $"Deleted and edited messages in channels where this feature is turned on will be logged in {channel.Mention}"));
                }
            }
        }

        [Command("On")]
        public async Task On(SocketTextChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(channel, new[] { ChannelPermission.ViewChannel }, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "MessageLogs-Channel", channel.Id.ToString());
                    SaveData(Context.Guild.Id.ToString(), "MessageLogs-Channel", channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Now logging channel", "Deleted and edited messages will be sent in the log channel\nMake sure you have a log channel set!"));
                }
            }
        }

        [Command("On")]
        public async Task On(string all)
        {
            if (all.ToLower() == "all")
            {
                if (Permission(Context.User, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "MessageLogs-DefaultOn");
                    SaveData(Context.Guild.Id.ToString(), "MessageLogs-DefaultOn", "true");
                    IDisposable typing = Context.Channel.EnterTypingState();

                    int failed = 0;

                    foreach (SocketTextChannel channel in Context.Guild.TextChannels)
                    {
                        if (BotHasPermissions(channel, new[] { ChannelPermission.ViewChannel }, Context.Channel, false))
                        {
                            DeleteData(Context.Guild.Id.ToString(), "MessageLogs-Channel", channel.Id.ToString());
                            SaveData(Context.Guild.Id.ToString(), "MessageLogs-Channel", channel.Id.ToString());
                        }
                        else failed += 1;
                    }

                    typing.Dispose();
                    if (failed == 0) await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Now logging all channels", "Deleted and edited messages will be sent in the log channel\nMake sure you have a log channel set!"));
                    else if (failed == 1) await Context.Channel.SendMessageAsync(embed: GetEmbed("Neutral", "Now logging most channels", $"Deleted and edited messages will be sent in the log channel\nMake sure you have a log channel set!\nI don't have the `ReadMessages` permission in {failed} channel"));
                    else await Context.Channel.SendMessageAsync(embed: GetEmbed("Neutral", "Now logging most channels", $"Deleted and edited messages will be sent in the log channel\nMake sure you have a log channel set!\nI don't have the `ReadMessages` permission in {failed} channels"));
                }
            }
            else
            {
                string prefix = ".";
                try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
            }
        }

        [Command("Off")]
        public async Task Off(SocketTextChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "MessageLogs-Channel", channel.Id.ToString());
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "No longer logging channel"));
            }
        }

        [Command("Off")]
        public async Task Off(string all)
        {
            if (all.ToLower() == "all")
            {
                DeleteData(Context.Guild.Id.ToString(), "MessageLogs-DefaultOn");

                if (Permission(Context.User, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "MessageLogs-Channel");

                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "All logging disabled"));
                }
            }
            else
            {
                string prefix = ".";
                try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
            }
        }
    }
}