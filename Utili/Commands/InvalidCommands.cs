using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;
using static Utili.SendMessage;

namespace Utili
{
    public class InvalidCommands : ModuleBase<SocketCommandContext>
    {
        [Command("On"), Alias("Enable")]
        public async Task On([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExamples:\n{Prefix}autopurge on [channel]\n{Prefix}antiprofane on"));
        }

        [Command("Off"), Alias("Disable")]
        public async Task Off([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExamples:\n{Prefix}autopurge off [channel]\n{Prefix}antiprofane off"));
        }

        [Command("Time")]
        public async Task Time([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExamples:\n{Prefix}autopurge time [channel] [timespan]\n{Prefix}inactive time [timespan]"));
        }

        [Command("Channel")]
        public async Task Channel([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}logs channel [channel]"));
        }

        [Command("upEmote")]
        public async Task UpEmote([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}votes upEmote [emote | reset]"));
        }

        [Command("downEmote")]
        public async Task DownEmote([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}votes downEmote [emote | reset]"));
        }

        [Command("Mode")]
        public async Task Mode([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}votes mode [all | attachments]"));
        }

        [Command("Title"), Alias("setTitle")]
        public async Task Title([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}notice title [channel] [message | none]"));
        }

        [Command("Content"), Alias("setContent")]
        public async Task Content([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}notice content [channel] [message | none]"));
        }

        [Command("NormalText"), Alias("setNormalText")]
        public async Task NormalText([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}notice normalText [channel] [message | none]"));
        }

        [Command("Colour"), Alias("setColour", "Color", "SetColor")]
        public async Task Colour([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}notice colour [channel] [R] [G] [B]"));
        }

        [Command("Image"), Alias("setImage")]
        public async Task Image([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}notice image [channel] [url | none]"));
        }

        [Command("Icon"), Alias("setIcon")]
        public async Task Icon([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}notice icon [channel] [url | none]"));
        }

        [Command("Thumbnail"), Alias("setThumbnail")]
        public async Task Thumbnail([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}notice thumbnail [channel] [url | none]"));
        }

        [Command("Delay"), Alias("setDelay")]
        public async Task Delay([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}notice delay [channel] [timespan]"));
        }

        [Command("Duplicate"), Alias("Copy", "Move")]
        public async Task Duplicate([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}notice duplicate [from channel] [to channel]"));
        }

        [Command("Threshold")]
        public async Task Threshold([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}spam threshold [integer]"));
        }

        [Command("Images")]
        public async Task Images([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}filter images [channel]"));
        }

        [Command("Videos")]
        public async Task Videos([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}filter videos [channel]"));
        }

        [Command("Media")]
        public async Task Media([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}filter media [channel]"));
        }

        [Command("Music")]
        public async Task Music([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}filter music [channel]"));
        }

        [Command("Attachments")]
        public async Task Attachments([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}filter attachments [channel]"));
        }

        [Command("Mirror")]
        public async Task Mirror([Remainder] string Args = "")
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

            await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command", $"To use this command, you need to prefix it with something.\n\nExample:\n{Prefix}mirroring mirror [from channel] [to channel]"));
        }
    }
}