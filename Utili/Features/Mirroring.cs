using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Webhook;

using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;
using static Utili.Json;
using Discord.Rest;
using Google.Apis.YouTube.v3.Data;
using System.Net.Sockets;

namespace Utili
{
    class Mirroring
    {
        public async Task Mirroring_MessageReceived(SocketMessage MessageParam)
        {
            var Message = MessageParam as SocketUserMessage;
            var Context = new SocketCommandContext(Program.Client, Message);

            if (Context.User.IsBot) return;

            var Data = GetData(Context.Guild.Id.ToString(), "Mirroring-Link");
            if (Data.Count == 0) return;

            foreach(var Value in Data)
            {
                try
                {
                    // If the from channel is this channel
                    if (Value.Value.Split(" -> ").First() == Context.Channel.Id.ToString())
                    {
                        SocketTextChannel ToChannel = null;
                        SocketGuild ToGuild = Context.Guild;

                        // If the link is in this guild only
                        if(!Value.Value.Split(" -> ").Last().Contains("G"))
                        {
                            ToChannel = Context.Guild.GetTextChannel(ulong.Parse(Value.Value.Split(" -> ").Last()));
                        }
                        // If the to channel is in another guild
                        else
                        {
                            ToGuild = Program.GlobalClient.GetGuild(ulong.Parse(Value.Value.Split("G").Last()));
                            ToChannel = ToGuild.GetTextChannel(ulong.Parse(Value.Value.Split(" -> ").Last().Split(" G").First()));
                        }

                        ulong WebhookID = 0;
                        try { WebhookID = ulong.Parse(GetData(ToChannel.Guild.Id.ToString(), $"Mirroring-WebhookID-{ToChannel.Id}").First().Value); }
                        catch { }

                        IWebhook Web = null;
                        bool Retry = false;

                        try { Web = await ToChannel.GetWebhookAsync(WebhookID); }
                        catch { Retry = true; }
                        if (Web == null) Retry = true;

                        if (Retry)
                        {
                            FileStream Avatar = File.OpenRead("Avatar.png");
                            Web = await ToChannel.CreateWebhookAsync("Utili Mirroring", Avatar);
                            Avatar.Close();
                            DeleteData(ToChannel.Guild.Id.ToString(), $"Mirroring-WebhookID-{ToChannel.Id}");
                            SaveData(ToChannel.Guild.Id.ToString(), $"Mirroring-WebhookID-{ToChannel.Id}", Web.Id.ToString());
                        }

                        await Task.Delay(500);

                        DiscordWebhookClient Webhook = new DiscordWebhookClient(Web);
                        string Username = $"{Context.User.Username}#{Context.User.Discriminator} [#{Context.Channel.Name} - {Context.Guild.Name}]";
                        string AvatarURL = Context.User.GetAvatarUrl();

                        await Task.Delay(500);

                        if (Context.Message.Content != null && Context.Message.Content != "")
                        {
                            if (Context.Message.Embeds.Count == 0) await Webhook.SendMessageAsync(Context.Message.Content, username: Username, avatarUrl: AvatarURL);
                            else await Webhook.SendMessageAsync(Context.Message.Content, embeds: Context.Message.Embeds, username: Username, avatarUrl: AvatarURL);
                        }

                        if (Context.Message.Attachments.Count != 0)
                        {
                            string Links = "";
                            foreach (string Attachment in Context.Message.Attachments.Select(x => x.Url)) Links += $"{Attachment}\n";

                            await Webhook.SendMessageAsync(Links, username: Username, avatarUrl: AvatarURL);
                        }
                    }
                }
                catch { }
            }
        }
    }

    [Group("Mirroring")]
    public class MirroringCommands : ModuleBase<SocketCommandContext>
    {
        public static string HelpContent =
                "help - Show this list\n" +
                "about - Display feature information\n" +
                "enable [from channel] [to channel] - Create a mirroring link\n" +
                "enable [from channel] [to guild id] [to channel id] - Create a cross-guild mirroring link\n" +
                "disable [from channel] [to channel] - Remove a mirroring link\n" +
                "disable [from channel] [to guild id] [to channel id] - Remove a cross-guild mirroring link\n" +
                "clear [channel] - Remove all mirroring links which involve this channel\n\n" +
                "allowPublic [channel] - Allow anyone to mirror this channel to their own guild\n" +
                "disallowPublic [channel] - Stop allowing anyone to mirror this channel to their own guild\n";

        [Command("Help")]
        public async Task Help()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel Mirroring", HelpContent, $"Prefix these commands with {Prefix}mirroring"));
        }

        [Command("")]
        public async Task Empty()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel Mirroring", HelpContent, $"Prefix these commands with {Prefix}mirroring"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel Mirroring", "When a message is sent by a user in one channel, it is copied by the bot to another channel."));
        }

        [Command("Mirror"), Alias("Enable")]
        public async Task Mirror(ITextChannel From, ITextChannel To)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(To, new ChannelPermission[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.ManageWebhooks }, Context.Channel))
                {
                    if (BotHasPermissions(From, new ChannelPermission[] { ChannelPermission.ViewChannel }, Context.Channel))
                    {
                        DeleteData(Context.Guild.Id.ToString(), "Mirroring-Link", $"{From.Id} -> {To.Id}");
                        SaveData(Context.Guild.Id.ToString(), "Mirroring-Link", $"{From.Id} -> {To.Id}");
                        await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Mirroring enabled", $"Now mirroring {From.Mention} to {To.Mention}"));
                    }
                }
            }
        }

        [Command("Disable"), Alias("Unmirror")]
        public async Task Disable(ITextChannel From, ITextChannel To)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "Mirroring-Link", $"{From.Id} -> {To.Id}");
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Mirroring disabled", $"No longer mirroring {From.Mention} to {To.Mention}"));
            }
        }

        [Command("Mirror"), Alias("Enable")]
        public async Task Mirror(ITextChannel From, ulong ToGuildID, ulong ToID)
        {
            bool PublicAllowed = false;
            if (GetData(Context.Guild.Id.ToString(), "Mirroring-Public", From.Id.ToString()).Count > 0) PublicAllowed = true;

            bool Allowed = PublicAllowed;
            if (!Allowed) Allowed = Permission(Context.User, Context.Channel);

            if (Allowed)
            {
                SocketGuild ToGuild;
                ITextChannel To;
                try
                {
                    ToGuild = Program.Client.GetGuild(ToGuildID);
                    To = ToGuild.GetTextChannel(ToID);
                    if (ToGuild.Id == Context.Guild.Id) throw new Exception();

                    if (!(Context.User as SocketGuildUser).GetPermissions(From).ViewChannel) throw new Exception();
                }
                catch
                {
                    string Prefix = ".";
                    try { Prefix = GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {Prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
                    return;
                }

                if(Permission(ToGuild.GetUser(Context.User.Id), Context.Channel, true))
                {
                    if (BotHasPermissions(To, new ChannelPermission[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.ManageWebhooks }, Context.Channel))
                    {
                        if (BotHasPermissions(From, new ChannelPermission[] { ChannelPermission.ViewChannel }, Context.Channel))
                        {
                            DeleteData(Context.Guild.Id.ToString(), "Mirroring-Link", $"{From.Id} -> {To.Id} G{ToGuild.Id}");
                            SaveData(Context.Guild.Id.ToString(), "Mirroring-Link", $"{From.Id} -> {To.Id} G{ToGuild.Id}");
                            await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Mirroring enabled", $"Now mirroring {From.Mention} to #{To.Name} in {ToGuild.Name}"));
                        }
                    }
                }
            }
        }

        [Command("Disable"), Alias("Unmirror")]
        public async Task Disable(ITextChannel From, ulong ToGuildID, ulong ToID)
        {
            if (Permission(Context.User, Context.Channel))
            {
                SocketGuild ToGuild = null;
                ITextChannel To = null;
                try
                {
                    ToGuild = Program.Client.GetGuild(ToGuildID);
                    To = ToGuild.GetTextChannel(ToID);
                    if (ToGuild.Id == Context.Guild.Id) throw new Exception();
                }
                catch
                {
                    string Prefix = ".";
                    try { Prefix = GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {Prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
                    return;
                }

                if (Permission(ToGuild.GetUser(Context.User.Id), Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "Mirroring-Link", $"{From.Id} -> {To.Id} G{ToGuild.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Mirroring disabled", $"No longer mirroring {From.Mention} to #{To.Name} in {ToGuild.Name}"));
                }
            }
        }

        [Command("Clear")]
        public async Task Clear(ITextChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                var Data = GetData(Type: "Mirroring-Link");
                int Links = 0;
                foreach(var Value in Data)
                {
                    if (Value.Value.Contains(Channel.Id.ToString()))
                    {
                        DeleteData(Value.GuildID, Value.Type, Value.Value);
                        Links += 1;
                    }
                }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Cleared mirroring links", $"Removed {Links} mirroring links which sent from or to this channel."));
            }
        }

        [Command("AllowPublic")]
        public async Task AllowPublic(ITextChannel Channel)
        {
            if(Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "Mirroring-Public", Channel.Id.ToString());
                SaveData(Context.Guild.Id.ToString(), "Mirroring-Public", Channel.Id.ToString());

                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Public mirroring allowed", "Anyone who can view this channel can now create a permanent mirroring link to their own guilds. Disable this immediately if you ever use this channel to send sensitive information."));
            }
        }

        [Command("DisallowPublic")]
        public async Task DisallowPublic(ITextChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "Mirroring-Public", Channel.Id.ToString());

                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Public mirroring disallowed", "Any existing mirroring links have not been removed. Now, only people with the manage guild permission on this guild can create mirroring links to their own guilds."));
            }
        }
    }
}
