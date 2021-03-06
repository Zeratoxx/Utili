﻿using System.Threading.Tasks;
using Discord.Commands;
using static Utili.SendMessage;

namespace Utili
{
    public class InvalidCommands : ModuleBase<SocketCommandContext>
    {
        [Command("On"), Alias("Enable")]
        public async Task On([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExamples:\n{prefix}autopurge on [channel]\n{prefix}antiprofane on"));
        }

        [Command("Off"), Alias("Disable")]
        public async Task Off([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExamples:\n{prefix}autopurge off [channel]\n{prefix}antiprofane off"));
        }

        [Command("Time")]
        public async Task Time([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExamples:\n{prefix}autopurge time [channel] [timespan]\n{prefix}inactive time [timespan]"));
        }

        [Command("Channel")]
        public async Task Channel([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}logs channel [channel]"));
        }

        [Command("upEmote")]
        public async Task UpEmote([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}votes upEmote [emote | reset]"));
        }

        [Command("downEmote")]
        public async Task DownEmote([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}votes downEmote [emote | reset]"));
        }

        [Command("Mode")]
        public async Task Mode([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}votes mode [all | attachments]"));
        }

        [Command("Title"), Alias("setTitle")]
        public async Task Title([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}notice title [channel] [message | none]"));
        }

        [Command("Content"), Alias("setContent")]
        public async Task Content([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}notice content [channel] [message | none]"));
        }

        [Command("NormalText"), Alias("setNormalText")]
        public async Task NormalText([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}notice normalText [channel] [message | none]"));
        }

        [Command("Colour"), Alias("setColour", "Color", "SetColor")]
        public async Task Colour([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}notice colour [channel] [R] [G] [B]"));
        }

        [Command("Image"), Alias("setImage")]
        public async Task Image([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}notice image [channel] [url | none]"));
        }

        [Command("Icon"), Alias("setIcon")]
        public async Task Icon([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}notice icon [channel] [url | none]"));
        }

        [Command("Thumbnail"), Alias("setThumbnail")]
        public async Task Thumbnail([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}notice thumbnail [channel] [url | none]"));
        }

        [Command("Delay"), Alias("setDelay")]
        public async Task Delay([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}notice delay [channel] [timespan]"));
        }

        [Command("Duplicate"), Alias("Copy", "Move")]
        public async Task Duplicate([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}notice duplicate [from channel] [to channel]"));
        }

        [Command("Threshold")]
        public async Task Threshold([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}spam threshold [integer]"));
        }

        [Command("Images")]
        public async Task Images([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}filter images [channel]"));
        }

        [Command("Videos")]
        public async Task Videos([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}filter videos [channel]"));
        }

        [Command("Media")]
        public async Task Media([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}filter media [channel]"));
        }

        [Command("Music")]
        public async Task Music([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}filter music [channel]"));
        }

        [Command("Attachments")]
        public async Task Attachments([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}filter attachments [channel]"));
        }

        [Command("Mirror")]
        public async Task Mirror([Remainder] string args = "")
        {
            string prefix = ".";
            try { prefix = Data.GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{prefix}mirroring mirror [from channel] [to channel]"));
        }
    }
}