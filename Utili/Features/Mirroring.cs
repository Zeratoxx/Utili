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

using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;
using static Utili.Json;

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
                    if (Value.Value.Split(" -> ").First() == Context.Channel.Id.ToString())
                    {
                        var Channel2 = Context.Guild.GetTextChannel(ulong.Parse(Value.Value.Split(" -> ").Last()));
                        await Channel2.SendMessageAsync($"**{Context.User.Username}#{Context.User.Discriminator}**\n{Context.Message.Content}");
                        if (Context.Message.Attachments.Count > 0)
                        {
                            string Content = "**Message Attachments**";
                            foreach (var Attachment in Context.Message.Attachments) Content += $"\n{Attachment.Url}";
                            await Channel2.SendMessageAsync(Content);
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
                "mirror [from channel] [to channel] - Create a mirroring link\n" +
                "disable [from channel] [to channel] - Remove a mirroring link";

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
                if (BotHasPermissions(To, new ChannelPermission[] { ChannelPermission.ViewChannel, ChannelPermission.SendMessages }, Context.Channel))
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
    }
}
