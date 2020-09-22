using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Webhook;
using Discord.WebSocket;
using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;

namespace Utili
{
    internal class Mirroring
    {
        public async Task Mirroring_MessageReceived(SocketMessage messageParam)
        {
            SocketUserMessage message = messageParam as SocketUserMessage;
            SocketCommandContext context = new SocketCommandContext(Program.Client, message);

            if (context.User.IsBot) return;

            List<Data> data = GetData(context.Guild.Id.ToString(), "Mirroring-Link");
            if (data.Count == 0) return;

            foreach (Data value in data)
            {
                try
                {
                    // If the from channel is this channel
                    if (value.Value.Split(" -> ").First() == context.Channel.Id.ToString())
                    {
                        SocketTextChannel toChannel = null;
                        SocketGuild toGuild = context.Guild;

                        // If the link is in this guild only
                        if (!value.Value.Split(" -> ").Last().Contains("G"))
                        {
                            toChannel = context.Guild.GetTextChannel(ulong.Parse(value.Value.Split(" -> ").Last()));
                        }
                        // If the to channel is in another guild
                        else
                        {
                            toGuild = Program.Shards.GetGuild(ulong.Parse(value.Value.Split("G").Last()));
                            toChannel = toGuild.GetTextChannel(ulong.Parse(value.Value.Split(" -> ").Last().Split(" G").First()));
                        }

                        ulong webhookId = 0;
                        try { webhookId = ulong.Parse(GetFirstData(toChannel.Guild.Id.ToString(), $"Mirroring-WebhookID-{toChannel.Id}").Value); }
                        catch { }

                        IWebhook web = null;
                        bool retry = false;

                        try { web = await toChannel.GetWebhookAsync(webhookId); }
                        catch { retry = true; }
                        if (web == null) retry = true;

                        if (retry)
                        {
                            FileStream avatar = File.OpenRead("Avatar.png");
                            web = await toChannel.CreateWebhookAsync("Utili Mirroring", avatar);
                            avatar.Close();
                            DeleteData(toChannel.Guild.Id.ToString(), $"Mirroring-WebhookID-{toChannel.Id}");
                            SaveData(toChannel.Guild.Id.ToString(), $"Mirroring-WebhookID-{toChannel.Id}", web.Id.ToString());
                        }

                        await Task.Delay(500);

                        DiscordWebhookClient webhook = new DiscordWebhookClient(web);
                        string username = $"{context.User.Username}#{context.User.Discriminator} [#{context.Channel.Name} - {context.Guild.Name}]";
                        string avatarUrl = context.User.GetAvatarUrl();

                        await Task.Delay(500);

                        if (context.Message.Content != null && context.Message.Content != "")
                        {
                            if (context.Message.Embeds.Count == 0) await webhook.SendMessageAsync(context.Message.Content, username: username, avatarUrl: avatarUrl);
                            else await webhook.SendMessageAsync(context.Message.Content, embeds: context.Message.Embeds, username: username, avatarUrl: avatarUrl);
                        }

                        if (context.Message.Attachments.Count != 0)
                        {
                            string links = "";
                            foreach (string attachment in context.Message.Attachments.Select(x => x.Url)) links += $"{attachment}\n";

                            await webhook.SendMessageAsync(links, username: username, avatarUrl: avatarUrl);
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
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel Mirroring", HelpContent, $"Prefix these commands with {prefix}mirroring"));
        }

        [Command("")]
        public async Task Empty()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel Mirroring", HelpContent, $"Prefix these commands with {prefix}mirroring"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Channel Mirroring", "When a message is sent by a user in one channel, it is copied by the bot to another channel."));
        }

        [Command("Mirror"), Alias("Enable")]
        public async Task Mirror(ITextChannel from, ITextChannel to)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(to, new[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.ManageWebhooks }, Context.Channel))
                {
                    if (BotHasPermissions(from, new[] { ChannelPermission.ViewChannel }, Context.Channel))
                    {
                        DeleteData(Context.Guild.Id.ToString(), "Mirroring-Link", $"{from.Id} -> {to.Id}");
                        SaveData(Context.Guild.Id.ToString(), "Mirroring-Link", $"{from.Id} -> {to.Id}");
                        await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Mirroring enabled", $"Now mirroring {from.Mention} to {to.Mention}"));
                    }
                }
            }
        }

        [Command("Disable"), Alias("Unmirror")]
        public async Task Disable(ITextChannel from, ITextChannel to)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "Mirroring-Link", $"{from.Id} -> {to.Id}");
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Mirroring disabled", $"No longer mirroring {from.Mention} to {to.Mention}"));
            }
        }

        [Command("Mirror"), Alias("Enable")]
        public async Task Mirror(ITextChannel from, ulong toGuildId, ulong toId)
        {
            bool publicAllowed = false;
            if (DataExists(Context.Guild.Id.ToString(), "Mirroring-Public", from.Id.ToString())) publicAllowed = true;

            bool allowed = publicAllowed;
            if (!allowed) allowed = Permission(Context.User, Context.Channel);

            if (allowed)
            {
                SocketGuild toGuild;
                ITextChannel to;
                try
                {
                    toGuild = Program.Shards.GetGuild(toGuildId);
                    to = toGuild.GetTextChannel(toId);
                    if (toGuild.Id == Context.Guild.Id) throw new Exception();

                    if (!(Context.User as SocketGuildUser).GetPermissions(from).ViewChannel) throw new Exception();
                }
                catch
                {
                    string prefix = ".";
                    try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
                    return;
                }

                if (Permission(toGuild.GetUser(Context.User.Id), Context.Channel, true))
                {
                    if (BotHasPermissions(to, new[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages, ChannelPermission.ManageWebhooks }, Context.Channel))
                    {
                        if (BotHasPermissions(from, new[] { ChannelPermission.ViewChannel }, Context.Channel))
                        {
                            DeleteData(Context.Guild.Id.ToString(), "Mirroring-Link", $"{from.Id} -> {to.Id} G{toGuild.Id}");
                            SaveData(Context.Guild.Id.ToString(), "Mirroring-Link", $"{from.Id} -> {to.Id} G{toGuild.Id}");
                            await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Mirroring enabled", $"Now mirroring {from.Mention} to #{to.Name} in {toGuild.Name}"));
                        }
                    }
                }
            }
        }

        [Command("Disable"), Alias("Unmirror")]
        public async Task Disable(ITextChannel from, ulong toGuildId, ulong toId)
        {
            if (Permission(Context.User, Context.Channel))
            {
                SocketGuild toGuild;
                ITextChannel to;
                try
                {
                    toGuild = Program.Shards.GetGuild(toGuildId);
                    to = toGuild.GetTextChannel(toId);
                    if (toGuild.Id == Context.Guild.Id) throw new Exception();
                }
                catch
                {
                    string prefix = ".";
                    try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
                    return;
                }

                if (Permission(toGuild.GetUser(Context.User.Id), Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "Mirroring-Link", $"{from.Id} -> {to.Id} G{toGuild.Id}");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Mirroring disabled", $"No longer mirroring {from.Mention} to #{to.Name} in {toGuild.Name}"));
                }
            }
        }

        [Command("Clear")]
        public async Task Clear(ITextChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                List<Data> data = GetData(type: "Mirroring-Link");
                int links = 0;
                foreach (Data value in data)
                {
                    if (value.Value.Contains(channel.Id.ToString()))
                    {
                        DeleteData(value.GuildId, value.Type, value.Value);
                        links += 1;
                    }
                }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Cleared mirroring links", $"Removed {links} mirroring links which sent from or to this channel."));
            }
        }

        [Command("AllowPublic")]
        public async Task AllowPublic(ITextChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "Mirroring-Public", channel.Id.ToString());
                SaveData(Context.Guild.Id.ToString(), "Mirroring-Public", channel.Id.ToString());

                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Public mirroring allowed", "Anyone who can view this channel can now create a permanent mirroring link to their own guilds. Disable this immediately if you ever use this channel to send sensitive information."));
            }
        }

        [Command("DisallowPublic")]
        public async Task DisallowPublic(ITextChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "Mirroring-Public", channel.Id.ToString());

                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Public mirroring disallowed", "Any existing mirroring links have not been removed. Now, only people with the manage guild permission on this guild can create mirroring links to their own guilds."));
            }
        }
    }
}