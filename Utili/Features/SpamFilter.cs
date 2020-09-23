using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using static Utili.Data;
using static Utili.Logic;
using static Utili.Program;
using static Utili.SendMessage;

namespace Utili
{
    internal class SpamFilter
    {
        public static List<(ulong, DateTime)> SpamTracker = new List<(ulong, DateTime)>();

        public async Task SpamFilter_MessageReceived(SocketMessage messageParam)
        {
            SocketUserMessage message = messageParam as SocketUserMessage;
            SocketCommandContext context = new SocketCommandContext(_client, message);

            if(!GetPerms(context.Channel).ManageMessages) return;

            if (context.User.IsBot || context.User.IsWebhook) return;

            if (DataExists(context.Guild.Id.ToString(), "SpamFilter-Enabled", "True"))
            {
                int threshold = 5;
                try { threshold = int.Parse(GetFirstData(context.Guild.Id.ToString(), "SpamFilter-Threshold").Value); } catch { }

                SpamTracker.Add((context.User.Id, DateTime.Now));

                SpamTracker.RemoveAll(x => x.Item2 < DateTime.Now - TimeSpan.FromSeconds(7));
                if (SpamTracker.Where(x => x.Item1 == context.User.Id).Count() >= threshold)
                {
                    await context.Message.DeleteAsync();
                }
            }
        }
    }

    [Group("Spam")]
    public class SpamFilterCommands : ModuleBase<SocketCommandContext>
    {
        public static string HelpContent =
                "help - Show this list\n" +
                "about - Display feature information\n" +
                "threshold [integer] - Set the detection threshold\n" +
                "on - Enable the feature in the server\n" +
                "off - Disable the feature in the server";

        [Command("Help")]
        public async Task Help()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Spam Filter", HelpContent, $"Prefix these commands with {prefix}spam"));
        }

        [Command("")]
        public async Task Empty()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Spam Filter", HelpContent, $"Prefix these commands with {prefix}spam"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Spam Filter", "This feature deletes messages that it thinks is spam. You can change how vigerous this filter is, the lower the threshold the easier it is to trigger the filter."));
        }

        [Command("Threshold")]
        public async Task Threshold(int threshold)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (threshold > 1 & threshold < 21)
                {
                    DeleteData(Context.Guild.Id.ToString(), "SpamFilter-Threshold");
                    SaveData(Context.Guild.Id.ToString(), "SpamFilter-Threshold", threshold.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Threshold set", "The lower the threshold the easier it is to trigger the filter\nThe recommended value is 5"));
                }
                else
                {
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid threshold", "Threshold must be between 2 and 20"));
                }
            }
        }

        [Command("On"), Alias("Enable")]
        public async Task On()
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (Context.Guild.GetUser(_client.CurrentUser.Id).GuildPermissions.ManageMessages)
                {
                    DeleteData(Context.Guild.Id.ToString(), "SpamFilter-Enabled");
                    SaveData(Context.Guild.Id.ToString(), "SpamFilter-Enabled", "True");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Spam filter enabled"));
                }
                else await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "I don't have permission", "I need the guild permission `ManageMessages` to do that\n[Support Discord](https://discord.gg/WsxqABZ)"));
            }
        }

        [Command("Off"), Alias("Disable")]
        public async Task Off()
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "SpamFilter-Enabled");
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Spam filter disabled"));
            }
        }
    }
}