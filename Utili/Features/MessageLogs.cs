using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;

namespace Utili
{
    internal class MessageLogs
    {
        public async Task MessageLogs_MessageReceived(SocketMessage MessageParam)
        {
            var Message = MessageParam as SocketUserMessage;
            var Context = new SocketCommandContext(Program.Client, Message);

            if (Context.User.IsBot || Context.User.IsWebhook) return;

            Random Random = new Random();

            if (DataExists(Context.Guild.Id.ToString(), "MessageLogs-Channel", Context.Channel.Id.ToString()))
            {
                await SaveMessageAsync(Context);

                // Every so often, delete message log channel entries for non-existant channels
                if (Random.Next(0, 10) == 5)
                {
                    var ChannelData = GetData(Context.Guild.Id.ToString(), "MessageLogs-Channel");
                    foreach (Data Channel in ChannelData)
                    {
                        if (!Context.Guild.TextChannels.Select(x => x.Id).Contains(ulong.Parse(Channel.Value)) && Context.Guild.TextChannels.Count > 0)
                        {
                            DeleteData(Context.Guild.Id.ToString(), "MessageLogs-Channel", Channel.Value);
                        }
                    }
                }
            }

            // Every so often, delete logs older than 30 days to comply with Discord's rules
            if (Random.Next(0, 25) == 10) RunNonQuery($"DELETE FROM Utili_MessageLogs WHERE(Timestamp < '{ToSQLTime(DateTime.Now - TimeSpan.FromDays(30))}')");
        }

        public async Task MessageLogs_MessageDeleted(Cacheable<IMessage, ulong> PartialMessage, ISocketMessageChannel Channel)
        {
            var Guild = (Channel as SocketTextChannel).Guild;
            if (DataExists(Guild.Id.ToString(), "MessageLogs-Channel", Channel.Id.ToString()))
            {
                MessageData Message = await GetMessageAsync(PartialMessage.Id);
                string Content = Decrypt(Message.EncryptedContent, ulong.Parse(Message.GuildID), ulong.Parse(Message.ChannelID));

                if (Message.ChannelID == Channel.Id.ToString())
                {
                    RunNonQuery($"DELETE FROM Utili_MessageLogs WHERE ID = {Message.ID}");
                    var LogChannel = Guild.GetTextChannel(ulong.Parse(GetFirstData(Guild.Id.ToString(), "MessageLogs-LogChannel").Value));
                    var User = Guild.GetUser(ulong.Parse(Message.UserID));

                    EmbedBuilder Embed = new EmbedBuilder();
                    Embed.WithColor(245, 66, 66);
                    Embed.WithCurrentTimestamp();
                    Embed.WithDescription($"**Message sent by {User.Mention} deleted in {(Channel as SocketTextChannel).Mention}**\n{Content}");

                    EmbedAuthorBuilder Author = new EmbedAuthorBuilder();
                    Author.Name = $"{User.Username}#{User.Discriminator}";
                    Author.IconUrl = User.GetAvatarUrl();
                    Embed.WithAuthor(Author);
                    Embed.WithFooter($"Message: {PartialMessage.Id} | User: {User.Id}");

                    await LogChannel.SendMessageAsync(embed: Embed.Build());
                }
            }
        }

        public async Task MessageLogs_MessageEdited(Cacheable<IMessage, ulong> PartialMessage, SocketMessage NewMessage, ISocketMessageChannel Channel)
        {
            var Guild = (Channel as SocketTextChannel).Guild;
            var Context = new SocketCommandContext(Program.Client, NewMessage as SocketUserMessage);

            if (DataExists(Guild.Id.ToString(), "MessageLogs-Channel", Channel.Id.ToString()))
            {
                MessageData Message = await GetMessageAsync(PartialMessage.Id);
                string Content = Decrypt(Message.EncryptedContent, ulong.Parse(Message.GuildID), ulong.Parse(Message.ChannelID));

                if (DataExists(Message.GuildID.ToString(), "MessageLogs-Channel", Message.ChannelID.ToString()))
                {
                    if (NewMessage.Content == Content) return;

                    RunNonQuery($"DELETE FROM Utili_MessageLogs WHERE ID = {Message.ID}");
                    var LogChannel = Guild.GetTextChannel(ulong.Parse(GetFirstData(Guild.Id.ToString(), "MessageLogs-LogChannel").Value));
                    var User = Guild.GetUser(ulong.Parse(Message.UserID));

                    EmbedBuilder Embed = new EmbedBuilder();
                    Embed.WithColor(66, 182, 245);
                    Embed.WithCurrentTimestamp();
                    Embed.WithDescription($"**Message sent by {User.Mention} edited in {(Channel as SocketTextChannel).Mention}**  [Jump]({NewMessage.GetJumpUrl()})");
                    Embed.AddField("Before", Content);
                    Embed.AddField("After", NewMessage.Content);
                    EmbedAuthorBuilder Author = new EmbedAuthorBuilder();
                    Author.Name = $"{User.Username}#{User.Discriminator}";
                    Author.IconUrl = User.GetAvatarUrl();
                    Embed.WithAuthor(Author);
                    Embed.WithFooter($"Message: {PartialMessage.Id} | User: {User.Id}");

                    await SaveMessageAsync(Context);

                    await LogChannel.SendMessageAsync(embed: Embed.Build());
                }
            }
        }

        public async Task MessageLogs_ChannelCreated(SocketChannel ChannelParam)
        {
            SocketTextChannel Channel = ChannelParam as SocketTextChannel;
            if (DataExists(Channel.Guild.Id.ToString(), "MessageLogs-DefaultOn"))
            {
                SaveData(Channel.Guild.Id.ToString(), "MessageLogs-Channel", Channel.Id.ToString());
            }
        }

        public static string Encrypt(string textData, ulong GuildID, ulong ChannelID)
        {
            string EncryptionKey = $"{GuildID * ChannelID}-{ChannelID * 3 - GuildID}";

            RijndaelManaged objrij = new RijndaelManaged();
            objrij.Mode = CipherMode.CBC;
            objrij.Padding = PaddingMode.PKCS7;
            objrij.KeySize = 0x80;
            objrij.BlockSize = 0x80;
            byte[] passBytes = Encoding.UTF8.GetBytes(EncryptionKey);
            byte[] EncryptionkeyBytes = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            int len = passBytes.Length;
            if (len > EncryptionkeyBytes.Length)
            {
                len = EncryptionkeyBytes.Length;
            }
            Array.Copy(passBytes, EncryptionkeyBytes, len);
            objrij.Key = EncryptionkeyBytes;
            objrij.IV = EncryptionkeyBytes;
            ICryptoTransform objtransform = objrij.CreateEncryptor();
            byte[] textDataByte = Encoding.UTF8.GetBytes(textData);
            return Convert.ToBase64String(objtransform.TransformFinalBlock(textDataByte, 0, textDataByte.Length));
        }

        public static string Decrypt(string EncryptedText, ulong GuildID, ulong ChannelID)
        {
            string EncryptionKey = $"{GuildID * ChannelID}-{ChannelID * 3 - GuildID}";

            RijndaelManaged objrij = new RijndaelManaged();
            objrij.Mode = CipherMode.CBC;
            objrij.Padding = PaddingMode.PKCS7;
            objrij.KeySize = 0x80;
            objrij.BlockSize = 0x80;
            byte[] encryptedTextByte = Convert.FromBase64String(EncryptedText);
            byte[] passBytes = Encoding.UTF8.GetBytes(EncryptionKey);
            byte[] EncryptionkeyBytes = new byte[0x10];
            int len = passBytes.Length;
            if (len > EncryptionkeyBytes.Length)
            {
                len = EncryptionkeyBytes.Length;
            }
            Array.Copy(passBytes, EncryptionkeyBytes, len);
            objrij.Key = EncryptionkeyBytes;
            objrij.IV = EncryptionkeyBytes;
            byte[] TextByte = objrij.CreateDecryptor().TransformFinalBlock(encryptedTextByte, 0, encryptedTextByte.Length);
            return Encoding.UTF8.GetString(TextByte);
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
            string Prefix = ".";
            try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Message Logs", HelpContent, $"Prefix these commands with {Prefix}logs"));
        }

        [Command("")]
        public async Task Empty()
        {
            string Prefix = ".";
            try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Message Logs", HelpContent, $"Prefix these commands with {Prefix}logs"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Message Logs", "Automatically posts messages in a log channel when they're deleted or edited. You can choose which channels it's turned on in, it's disabled by default.\nThe message content is always securely encrypted and is removed from the database once the message is deleted or edited.\nMessages are stored for up to 30 days."));
        }

        [Command("Channel")]
        public async Task LogChannel(ITextChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(Channel, new ChannelPermission[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages }, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "MessageLogs-LogChannel");
                    SaveData(Context.Guild.Id.ToString(), "MessageLogs-LogChannel", Channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set message logs channel", $"Deleted and edited messages in channels where this feature is turned on will be logged in {Channel.Mention}"));
                }
            }
        }

        [Command("On")]
        public async Task On(SocketTextChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(Channel, new ChannelPermission[] { ChannelPermission.ViewChannel }, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "MessageLogs-Channel", Channel.Id.ToString());
                    SaveData(Context.Guild.Id.ToString(), "MessageLogs-Channel", Channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Now logging channel", $"Deleted and edited messages will be sent in the log channel\nMake sure you have a log channel set!"));
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
                    var Typing = Context.Channel.EnterTypingState();

                    int Failed = 0;

                    foreach (var Channel in Context.Guild.TextChannels)
                    {
                        if (BotHasPermissions(Channel, new ChannelPermission[] { ChannelPermission.ViewChannel }, Context.Channel, false))
                        {
                            DeleteData(Context.Guild.Id.ToString(), "MessageLogs-Channel", Channel.Id.ToString());
                            SaveData(Context.Guild.Id.ToString(), "MessageLogs-Channel", Channel.Id.ToString());
                        }
                        else Failed += 1;
                    }

                    Typing.Dispose();
                    if (Failed == 0) await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Now logging all channels", $"Deleted and edited messages will be sent in the log channel\nMake sure you have a log channel set!"));
                    else if (Failed == 1) await Context.Channel.SendMessageAsync(embed: GetEmbed("Neutral", "Now logging most channels", $"Deleted and edited messages will be sent in the log channel\nMake sure you have a log channel set!\nI don't have the `ReadMessages` permission in {Failed} channel"));
                    else await Context.Channel.SendMessageAsync(embed: GetEmbed("Neutral", "Now logging most channels", $"Deleted and edited messages will be sent in the log channel\nMake sure you have a log channel set!\nI don't have the `ReadMessages` permission in {Failed} channels"));
                }
            }
            else
            {
                string Prefix = ".";
                try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {Prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
            }
        }

        [Command("Off")]
        public async Task Off(SocketTextChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "MessageLogs-Channel", Channel.Id.ToString());
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
                string Prefix = ".";
                try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {Prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
            }
        }
    }
}