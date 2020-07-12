using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Threading;
using System.Text;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;
using static Utili.Program;
using static Utili.Json;
using Renci.SshNet.Messages;

namespace Utili
{
    class Autopurge
    {
        List<Task> Tasks = new List<Task>();

        public static System.Timers.Timer StartRunthrough;

        public async Task Run()
        {
            StartRunthrough = new System.Timers.Timer(5000);
            StartRunthrough.Elapsed += StartRunthrough_Elapsed;
            StartRunthrough.Start();
        }

        private void StartRunthrough_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Ready)
            {
                Tasks.RemoveAll(x => x.IsCompleted);
                if (Tasks.Count <= GetMaxWorkers()) Tasks.Add(Process());
            }
        }

        public async Task Process()
        {
            List<Data> AllChannels = GetData(Type: "Autopurge-Channel");
            List<ulong> AllGuilds = new List<ulong>();

            foreach (Data Data in AllChannels)
            {
                if (!AllGuilds.Contains(ulong.Parse(Data.GuildID))) AllGuilds.Add(ulong.Parse(Data.GuildID));
            }

            foreach (ulong GuildID in AllGuilds)
            {
                try
                {
                    SocketGuild Guild = Client.GetGuild(GuildID);
                    List<Data> GuildChannels = AllChannels.Where(x => x.GuildID == GuildID.ToString()).ToList();

                    foreach (Data Data in GuildChannels)
                    {
                        try
                        {
                            SocketTextChannel Channel = Guild.GetTextChannel(ulong.Parse(Data.Value));
                            List<IMessage> MessagesToDelete = new List<IMessage>();

                            TimeSpan TimeSpan = TimeSpan.Parse("00:15:00");
                            try { TimeSpan = TimeSpan.Parse(GetData(GuildID.ToString(), $"Autopurge-Timespan-{Channel.Id}").First().Value); } catch { }

                            bool BotsOnly = false;
                            if (GetData(Guild.Id.ToString(), $"Autopurge-Mode-{Channel.Id}", "Bots").Count > 0) BotsOnly = true;

                            var Messages = await Channel.GetMessagesAsync(1000).FlattenAsync();

                            MessagesToDelete.AddRange(Messages);

                            if (BotsOnly) MessagesToDelete.RemoveAll(x => !x.Author.IsBot);
                            MessagesToDelete.RemoveAll(x => DateTime.Now - x.Timestamp.LocalDateTime < TimeSpan);
                            MessagesToDelete.RemoveAll(x => DateTime.Now - x.Timestamp.LocalDateTime >= TimeSpan.FromHours(335)); // 14 days - 1 hr
                            MessagesToDelete.RemoveAll(x => x.IsPinned);

                            await Channel.DeleteMessagesAsync(MessagesToDelete);
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }
    }

    [Group("Autopurge")]
    public class AutopurgeCommands : ModuleBase<SocketCommandContext>
    {
        public static string HelpContent =
                "help - Show this list\n" +
                "about - Display feature information\n" +
                "time [channel] [timespan] - Set the age at which messages are deleted in a channel\n" +
                "mode [channe] [all|bots] - Set whether all messages or bot messages are deleted\n" +
                "on [channel] - Enable autopurge in a channel\n" +
                "off [channel] - Disable autopurge in a channel";

        [Command("Help")]
        public async Task Help()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Autopurge", HelpContent, $"Prefix these commands with {Prefix}autopurge"));
        }

        [Command("")]
        public async Task Empty()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Autopurge", HelpContent, $"Prefix these commands with {Prefix}autopurge"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Autopurge", "In channels where this feature is enabled, messages older than the set time will be deleted automatically. This feature doesn't delete pinned messages."));
        }

        [Command("Time"), Alias("Timespan")]
        public async Task Time(ITextChannel Channel, TimeSpan Time)
        {
            if(Permission(Context.User, Context.Channel))
            {
                if(Time > TimeSpan.FromDays(13) + TimeSpan.FromHours(23))
                {
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"The maximum autopurge timespan is 13 days and 23 hours. (13d23h)"));
                    return;
                }

                DeleteData(Context.Guild.Id.ToString(), $"Autopurge-Timespan-{Channel.Id}");
                SaveData(Context.Guild.Id.ToString(), $"Autopurge-Timespan-{Channel.Id}", Time.ToString());
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set autopurge time", $"Messages older than {DisplayTimespan(Time)} will be deleted.\nThis setting is only for {Channel.Mention}.\nNote that messages are only purged every so often so this timer may not be completely accurate."));
            }
        }

        [Command("Mode")]
        public async Task Mode(ITextChannel Channel, string Mode)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if(Mode.ToLower() == "all")
                {
                    DeleteData(Context.Guild.Id.ToString(), $"Autopurge-Mode-{Channel.Id}");
                    SaveData(Context.Guild.Id.ToString(), $"Autopurge-Mode-{Channel.Id}", "All");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set autopurge mode", $"All messages (except pinned messages) will be deleted."));
                }
                else if(Mode.ToLower() == "bots" || Mode.ToLower() == "bot")
                {
                    DeleteData(Context.Guild.Id.ToString(), $"Autopurge-Mode-{Channel.Id}");
                    SaveData(Context.Guild.Id.ToString(), $"Autopurge-Mode-{Channel.Id}", "Bots");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set autopurge mode", $"All messages sent by bots (except pinned messages) will be deleted."));
                }
            }
        }

        [Command("On"), Alias("Enable")]
        public async Task On(ITextChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(Channel, new ChannelPermission[] { ChannelPermission.ViewChannel, ChannelPermission.ManageMessages }, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "Autopurge-Channel", Channel.Id.ToString());
                    SaveData(Context.Guild.Id.ToString(), "Autopurge-Channel", Channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Autopurge enabled", $"Autopurge enabled in {Channel.Mention}"));
                }  
            }
        }

        [Command("Off"), Alias("Disable")]
        public async Task Off(ITextChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "Autopurge-Channel", Channel.Id.ToString());
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Autopurge disabled", $"Autopurge disabled in {Channel.Mention}"));
            }
        }
    }
}
