using Discord;
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
    internal class Autopurge
    {
        private List<Task> Tasks = new List<Task>();

        public static System.Timers.Timer StartRunthrough;

        public async Task Run()
        {
            StartRunthrough = new System.Timers.Timer(10000);
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
                await Task.Delay(100);

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
                            try { TimeSpan = TimeSpan.Parse(GetFirstData(GuildID.ToString(), $"Autopurge-Timespan-{Channel.Id}").Value); } catch { }

                            bool BotsOnly = false;
                            if (DataExists(Guild.Id.ToString(), $"Autopurge-Mode-{Channel.Id}", "Bots")) BotsOnly = true;

                            var Messages = await Channel.GetMessagesAsync(10000).FlattenAsync();

                            foreach(var Message in Messages)
                            {
                                bool Delete = true;

                                TimeSpan MessageAge = DateTime.Now - Message.Timestamp.LocalDateTime;

                                // Don't delete if the message is younger than the desired timespan
                                if (MessageAge < TimeSpan) Delete = false;

                                // Don't delete if the message can't be deleted (too old)
                                if (MessageAge > TimeSpan.FromHours(335.75)) Delete = false;

                                // Don't delete if the message is pinned
                                if (Message.IsPinned) Delete = false;

                                if (BotsOnly)
                                {
                                    // Don't delete if the message was sent by a human
                                    if (!Message.Author.IsBot) Delete = false;
                                }

                                if (Delete) MessagesToDelete.Add(Message);
                            }

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
            try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Autopurge", HelpContent, $"Prefix these commands with {Prefix}autopurge"));
        }

        [Command("")]
        public async Task Empty()
        {
            string Prefix = ".";
            try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
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
            if (Permission(Context.User, Context.Channel))
            {
                if (Time > TimeSpan.FromDays(13) + TimeSpan.FromHours(23))
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
                if (Mode.ToLower() == "all")
                {
                    DeleteData(Context.Guild.Id.ToString(), $"Autopurge-Mode-{Channel.Id}");
                    SaveData(Context.Guild.Id.ToString(), $"Autopurge-Mode-{Channel.Id}", "All");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set autopurge mode", $"All messages (except pinned messages) will be deleted."));
                }
                else if (Mode.ToLower() == "bots" || Mode.ToLower() == "bot")
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