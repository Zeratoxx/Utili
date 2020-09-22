using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;

namespace Utili
{
    public class OwnerCommands : ModuleBase<SocketCommandContext>
    {
        [Command("Servers")]
        public async Task About(int number)
        {
            if (OwnerPermission(Context.User, Context.Channel))
            {
                IOrderedEnumerable<SocketGuild> servers = Program._shards.Guilds.OrderBy(x => x.Name);

                int oldNumber = number;
                number = (number - 1) * 10;

                string content = "";
                decimal d = servers.Count();
                decimal temp = d / 10m;
                decimal pages = Math.Ceiling(temp);

                int i = 0;
                int read = 0;
                bool reading = false;

                foreach (SocketGuild server in servers)
                {
                    if (!reading)
                    {
                        if (i == number) reading = true;
                    }
                    if (reading)
                    {
                        if (read == 10) reading = false;
                        else
                        {
                            content += $"{i + 1}. {server.Name} ({server.Id})\n";
                            read += 1;
                        }
                    }
                    i++;
                }

                if (content == "")
                {
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid page", $"There are pages 1-{pages}"));
                }
                else
                {
                    await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Servers", content, $"Page {oldNumber} of {pages}"));
                }
            }
        }

        [Command("ServerReset")]
        public async Task Reset([Remainder] string confirm = "")
        {
            if (OwnerPermission(Context.User, Context.Channel))
            {
                if (confirm.ToLower() == "confirm")
                {
                    if (BotHasPermissions(Context.Guild, new[] { GuildPermission.Administrator }, Context.Channel))
                    {
                        #region Clear

                        foreach (SocketCategoryChannel cat in Context.Guild.CategoryChannels)
                        {
                            await cat.DeleteAsync();
                        }
                        foreach (SocketGuildChannel channel in Context.Guild.Channels)
                        {
                            await channel.DeleteAsync();
                        }
                        foreach (SocketRole dRole in Context.Guild.Roles)
                        {
                            try { await dRole.DeleteAsync(); }
                            catch { try { await dRole.ModifyAsync(x => x.Permissions = new GuildPermissions()); } catch { } }
                        }

                        #endregion Clear

                        #region Channels

                        await Context.Guild.CreateTextChannelAsync("General");

                        RestCategoryChannel category = await Context.Guild.CreateCategoryChannelAsync("Bots");

                        foreach (SocketGuildUser bot in Context.Guild.Users.Where(x => x.IsBot).OrderBy(x => x.Username))
                        {
                            RestTextChannel channel = await Context.Guild.CreateTextChannelAsync($"{bot.Username}");
                            await channel.ModifyAsync(x => x.CategoryId = category.Id);
                            await channel.AddPermissionOverwriteAsync(bot, OverwritePermissions.AllowAll(channel));

                            RestVoiceChannel channel2 = await Context.Guild.CreateVoiceChannelAsync($"{bot.Username}");
                            await channel2.ModifyAsync(x => x.CategoryId = category.Id);
                            await channel2.AddPermissionOverwriteAsync(bot, OverwritePermissions.AllowAll(channel2));
                        }

                        #endregion Channels

                        #region Human Roles

                        RestRole role = await Context.Guild.CreateRoleAsync("Tester", new GuildPermissions(administrator: true), Color.Green, true, false);
                        foreach (SocketGuildUser user in Context.Guild.Users.Where(x => !x.IsBot))
                        {
                            await user.AddRoleAsync(role);
                        }

                        #endregion Human Roles
                    }
                }
                else
                {
                    string prefix = ".";
                    try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }

                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Warning", $"This command will **DELETE ALL CHANNELS AND ROLES**.\nThis **CAN NOT BE UNDONE**.\n\nThis command re-formats the server into a server for testing bots.\nUse {prefix}TestServerReset Confirm to continue.\n[Support Discord](https://discord.gg/WsxqABZ)"));
                }
            }
        }

        [Command("GuildInfo")]
        public async Task GuildInfo(ulong guildId)
        {
            if (OwnerPermission(Context.User, Context.Channel))
            {
                await Context.Channel.SendMessageAsync(embed: Logic.GuildInfo(Program._shards.GetGuild(guildId)));
            }
        }

        [Command("AddVoteLink")]
        public async Task AddVoteLink(string title, string content)
        {
            if (OwnerPermission(Context.User, Context.Channel))
            {
                SaveData(title, "VoteLink", content);
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Vote link saved", $"[{title}]({content})"));
            }
        }

        [Command("RemoveVoteLink")]
        public async Task RemoveVoteLink(string title, string content)
        {
            if (OwnerPermission(Context.User, Context.Channel))
            {
                DeleteData(title, "VoteLink", content);
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Vote link deleted", $"[{title}]({content})"));
            }
        }

        [Command("Delete")]
        public async Task Delete(string guildId = null, string type = null, string value = null)
        {
            if (OwnerPermission(Context.User, Context.Channel))
            {
                DeleteData(guildId, type, value);
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Deleted data"));
            }
        }

        [Command("Save")]
        public async Task Save(string guildId, string type, string value)
        {
            if (OwnerPermission(Context.User, Context.Channel))
            {
                SaveData(guildId, type, value);
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Saved data"));
            }
        }

        [Command("ForceCrash")]
        public async Task ForceCrash()
        {
            if (OwnerPermission(Context.User, Context.Channel))
            {
                Console.WriteLine($"[{DateTime.Now}] [Info] Script terminated via force crash command.");
                Program.Ready = false;
                Program.ForceStop.Cancel();
            }
        }
    }
}