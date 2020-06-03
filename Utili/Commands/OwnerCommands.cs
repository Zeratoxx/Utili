using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
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
    public class OwnerCommands : ModuleBase<SocketCommandContext>
    {
        [Command("Servers")]
        public async Task About(int Number)
        {
            if (OwnerPermission(Context.User, Context.Channel))
            {
                var Servers = Program.GlobalClient.Guilds.OrderBy(x => x.Name);

                int OldNumber = Number;
                Number = (Number - 1) * 10;

                string Content = "";
                decimal d = Servers.Count();
                decimal temp = d / 10m;
                decimal Pages = Math.Ceiling(temp);

                int i = 0;
                int Read = 0;
                bool Reading = false;

                foreach (var Server in Servers)
                {
                    if (!Reading)
                    {
                        if (i == Number) Reading = true;
                    }
                    if (Reading)
                    {
                        if (Read == 10) Reading = false;
                        else
                        {
                            Content += $"{i + 1}. {Server.Name} ({Server.Id})\n";
                            Read += 1;
                        }
                    }
                    i++;
                }

                if (Content == "")
                {
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid page", $"There are pages 1-{Pages}"));
                }
                else
                {
                    await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Servers", Content, $"Page {OldNumber} of {Pages}"));
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
                    if (BotHasPermissions(Context.Guild, new GuildPermission[] { GuildPermission.Administrator }, Context.Channel))
                    {
                        #region Clear

                        foreach (var Cat in Context.Guild.CategoryChannels)
                        {
                            await Cat.DeleteAsync();
                        }
                        foreach (var Channel in Context.Guild.Channels)
                        {
                            await Channel.DeleteAsync();
                        }
                        foreach (var DRole in Context.Guild.Roles)
                        {
                            try { await DRole.DeleteAsync(); }
                            catch { try { await DRole.ModifyAsync(x => x.Permissions = new GuildPermissions()); } catch { } };
                        }

                        #endregion

                        #region Channels

                        await Context.Guild.CreateTextChannelAsync("General");

                        var Category = await Context.Guild.CreateCategoryChannelAsync($"Bots");

                        foreach (var Bot in Context.Guild.Users.Where(x => x.IsBot).OrderBy(x => x.Username))
                        {
                            var Channel = await Context.Guild.CreateTextChannelAsync($"{Bot.Username}");
                            await Channel.ModifyAsync(x => x.CategoryId = Category.Id);
                            await Channel.AddPermissionOverwriteAsync(Bot, OverwritePermissions.AllowAll(Channel));

                            var Channel2 = await Context.Guild.CreateVoiceChannelAsync($"{Bot.Username}");
                            await Channel2.ModifyAsync(x => x.CategoryId = Category.Id);
                            await Channel2.AddPermissionOverwriteAsync(Bot, OverwritePermissions.AllowAll(Channel2));
                        }

                        #endregion

                        #region Human Roles

                        var Role = await Context.Guild.CreateRoleAsync("Tester", new GuildPermissions(administrator: true), Color.Green, true, false);
                        foreach (var User in Context.Guild.Users.Where(x => !x.IsBot))
                        {
                            await User.AddRoleAsync(Role);
                        }

                        #endregion
                    }
                }
                else 
                {
                    string Prefix = ".";
                    try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }

                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Warning", $"This command will **DELETE ALL CHANNELS AND ROLES**.\nThis **CAN NOT BE UNDONE**.\n\nThis command re-formats the server into a server for testing bots.\nUse {Prefix}TestServerReset Confirm to continue.\n[Support Discord](https://discord.gg/WsxqABZ)")); 
                }
            }
        }

        [Command("GuildInfo")]
        public async Task GuildInfo(ulong GuildID)
        {
            if (OwnerPermission(Context.User, Context.Channel))
            {
                await Context.Channel.SendMessageAsync(embed: Logic.GuildInfo(Program.GlobalClient.GetGuild(GuildID)));
            }
        }

        [Command("AddVoteLink")]
        public async Task AddVoteLink(string Title, string Content)
        {
            if(OwnerPermission(Context.User, Context.Channel))
            {
                SaveData(Title, "VoteLink", Content);
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Vote link saved", $"[{Title}]({Content})"));
            }
        }

        [Command("RemoveVoteLink")]
        public async Task RemoveVoteLink(string Title, string Content)
        {
            if (OwnerPermission(Context.User, Context.Channel))
            {
                DeleteData(Title, "VoteLink", Content);
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Vote link deleted", $"[{Title}]({Content})"));
            }
        }

        [Command("Delete")]
        public async Task Delete(string GuildID = null, string Type = null, string Value = null)
        {
            if (OwnerPermission(Context.User, Context.Channel))
            {
                DeleteData(GuildID, Type, Value);
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Deleted data"));
            }
        }

        [Command("Save")]
        public async Task Save(string GuildID, string Type, string Value)
        {
            if (OwnerPermission(Context.User, Context.Channel))
            {
                SaveData(GuildID, Type, Value);
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
