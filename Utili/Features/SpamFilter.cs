using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Utili.Data;
using static Utili.Logic;
using static Utili.Program;
using static Utili.SendMessage;

namespace Utili
{
    internal class SpamFilter
    {
        public static List<(ulong, DateTime)> SpamTracker = new List<(ulong, DateTime)>();

        public async Task SpamFilter_MessageReceived(SocketMessage MessageParam)
        {
            var Message = MessageParam as SocketUserMessage;
            var Context = new SocketCommandContext(Client, Message);

            if (Context.User.Id == Program.Client.CurrentUser.Id || Context.User.IsBot) return;

            if (GetData(Context.Guild.Id.ToString(), "SpamFilter-Enabled", "True").Count > 0)
            {
                int Threshold = 5;
                try { Threshold = int.Parse(GetData(Context.Guild.Id.ToString(), "SpamFilter-Threshold").First().Value); } catch { }

                if (!Context.User.IsBot & !Context.User.IsWebhook)
                {
                    SpamTracker.Add((Context.User.Id, Context.Message.Timestamp.DateTime));

                    SpamTracker.RemoveAll(x => x.Item2.CompareTo(DateTime.Now.AddSeconds(-7)) < 0);
                    if (SpamTracker.Where(x => x.Item1 == Context.User.Id).Count() >= Threshold)
                    {
                        await Context.Message.DeleteAsync();
                    }
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
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Spam Filter", HelpContent, $"Prefix these commands with {Prefix}spam"));
        }

        [Command("")]
        public async Task Empty()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Spam Filter", HelpContent, $"Prefix these commands with {Prefix}spam"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Spam Filter", "This feature deletes messages that it thinks is spam. You can change how vigerous this filter is, the lower the threshold the easier it is to trigger the filter."));
        }

        [Command("Threshold")]
        public async Task Threshold(int Threshold)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (Threshold > 1 & Threshold < 21)
                {
                    DeleteData(Context.Guild.Id.ToString(), "SpamFilter-Threshold");
                    SaveData(Context.Guild.Id.ToString(), "SpamFilter-Threshold", Threshold.ToString());
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
                if (Context.Guild.GetUser(Program.Client.CurrentUser.Id).GuildPermissions.ManageMessages)
                {
                    DeleteData(Context.Guild.Id.ToString(), "SpamFilter-Enabled");
                    SaveData(Context.Guild.Id.ToString(), "SpamFilter-Enabled", "True");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Spam filter enabled"));
                }
                else await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "I don't have permission", $"I need the guild permission `ManageMessages` to do that\n[Support Discord](https://discord.gg/WsxqABZ)"));
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