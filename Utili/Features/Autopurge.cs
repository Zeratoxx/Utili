using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using static Utili.Data;
using static Utili.Logic;
using static Utili.Program;
using static Utili.SendMessage;

namespace Utili
{
    internal class Autopurge
    {
        private readonly List<Task> _tasks = new List<Task>();

        public static Timer StartRunthrough;

        public async Task Run()
        {
            StartRunthrough = new Timer(5000);
            StartRunthrough.Elapsed += StartRunthrough_Elapsed;
            StartRunthrough.Start();
        }

        private void StartRunthrough_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Ready)
            {
                _tasks.RemoveAll(x => x.IsCompleted);
                if (_tasks.Count < GetMaxWorkers()) _tasks.Add(Process());
            }
        }

        public async Task Process()
        {
            List<Data> allChannels = GetData(type: "Autopurge-Channel");
            List<ulong> allGuilds = new List<ulong>();

            foreach (Data data in allChannels)
            {
                if (!allGuilds.Contains(ulong.Parse(data.GuildId))) allGuilds.Add(ulong.Parse(data.GuildId));
            }

            foreach (ulong guildId in allGuilds)
            {
                try
                {
                    SocketGuild guild = _client.GetGuild(guildId);
                    List<Data> guildChannels = allChannels.Where(x => x.GuildId == guildId.ToString()).ToList();

                    foreach (Data data in guildChannels)
                    {
                        try
                        {
                            SocketTextChannel channel = guild.GetTextChannel(ulong.Parse(data.Value));
                            List<IMessage> messagesToDelete = new List<IMessage>();

                            if(!GetPerms(channel).ManageMessages) return;

                            TimeSpan timeSpan = TimeSpan.Parse("00:15:00");
                            try { timeSpan = TimeSpan.Parse(GetFirstData(guildId.ToString(), $"Autopurge-Timespan-{channel.Id}").Value); } catch { }

                            bool botsOnly = false;
                            if (DataExists(guild.Id.ToString(), $"Autopurge-Mode-{channel.Id}", "Bots")) botsOnly = true;

                            IEnumerable<IMessage> messages = await channel.GetMessagesAsync(1000).FlattenAsync();

                            foreach (IMessage message in messages)
                            {
                                bool delete = true;

                                TimeSpan messageAge = DateTime.Now - message.Timestamp.LocalDateTime;

                                // Don't delete if the message is younger than the desired timespan
                                if (messageAge < timeSpan) delete = false;

                                // Don't delete if the message can't be deleted (too old)
                                if (messageAge > TimeSpan.FromHours(335.75)) delete = false;

                                // Don't delete if the message is pinned
                                if (message.IsPinned) delete = false;

                                if (botsOnly)
                                {
                                    // Don't delete if the message was sent by a human
                                    if (!message.Author.IsBot) delete = false;
                                }

                                if (delete) messagesToDelete.Add(message);
                            }

                            await channel.DeleteMessagesAsync(messagesToDelete);
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
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Autopurge", HelpContent, $"Prefix these commands with {prefix}autopurge"));
        }

        [Command("")]
        public async Task Empty()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Autopurge", HelpContent, $"Prefix these commands with {prefix}autopurge"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Autopurge", "In channels where this feature is enabled, messages older than the set time will be deleted automatically. This feature doesn't delete pinned messages."));
        }

        [Command("Time"), Alias("Timespan")]
        public async Task Time(ITextChannel channel, TimeSpan time)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (time > TimeSpan.FromDays(13) + TimeSpan.FromHours(23))
                {
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", "The maximum autopurge timespan is 13 days and 23 hours. (13d23h)"));
                    return;
                }

                DeleteData(Context.Guild.Id.ToString(), $"Autopurge-Timespan-{channel.Id}");
                SaveData(Context.Guild.Id.ToString(), $"Autopurge-Timespan-{channel.Id}", time.ToString());
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set autopurge time", $"Messages older than {DisplayTimespan(time)} will be deleted.\nThis setting is only for {channel.Mention}.\nNote that messages are only purged every so often so this timer may not be completely accurate."));
            }
        }

        [Command("Mode")]
        public async Task Mode(ITextChannel channel, string mode)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (mode.ToLower() == "all")
                {
                    DeleteData(Context.Guild.Id.ToString(), $"Autopurge-Mode-{channel.Id}");
                    SaveData(Context.Guild.Id.ToString(), $"Autopurge-Mode-{channel.Id}", "All");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set autopurge mode", "All messages (except pinned messages) will be deleted."));
                }
                else if (mode.ToLower() == "bots" || mode.ToLower() == "bot")
                {
                    DeleteData(Context.Guild.Id.ToString(), $"Autopurge-Mode-{channel.Id}");
                    SaveData(Context.Guild.Id.ToString(), $"Autopurge-Mode-{channel.Id}", "Bots");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set autopurge mode", "All messages sent by bots (except pinned messages) will be deleted."));
                }
            }
        }

        [Command("On"), Alias("Enable")]
        public async Task On(ITextChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(channel, new[] { ChannelPermission.ViewChannel, ChannelPermission.ManageMessages }, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "Autopurge-Channel", channel.Id.ToString());
                    SaveData(Context.Guild.Id.ToString(), "Autopurge-Channel", channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Autopurge enabled", $"Autopurge enabled in {channel.Mention}"));
                }
            }
        }

        [Command("Off"), Alias("Disable")]
        public async Task Off(ITextChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "Autopurge-Channel", channel.Id.ToString());
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Autopurge disabled", $"Autopurge disabled in {channel.Mention}"));
            }
        }
    }
}