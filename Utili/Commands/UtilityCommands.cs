using System;
using System.Collections.Generic;
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
    public class UtilityCommands : ModuleBase<SocketCommandContext>
    {
        #region Prune

        public static string PruneHelpContent =
            "prune [amount] - Prune an amount of messages\n" +
            "prune [amount] [user | userID] - Prune an amount of messages from a user\n" +
            "prune [amount] skip [skip] - Prune an amount of messages after skipping an amount of messages up the channel";

        [Command("Prune"), Alias("Purge", "Clear")]
        public async Task Prune()
        {
            if (MessagePermission(Context.User, Context.Channel, Context.Channel))
            {
                await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Prune Command", PruneHelpContent));
            }
        }

        [Command("Prune"), Alias("Purge", "Clear")]
        public async Task Prune(int amount)
        {
            amount += 1; //Account for the command message
            if (MessagePermission(Context.User, Context.Channel, Context.Channel))
            {
                if (BotHasPermissions(Context.Channel as ITextChannel, new[] { ChannelPermission.ManageMessages }, Context.Channel))
                {
                    bool sendWarning = false;

                    IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(amount * 1000).FlattenAsync();
                    messages = messages.Where(x => !x.IsPinned);

                    if (messages.Where(x => x.CreatedAt > DateTime.Now.AddDays(-14)).Count() != messages.Count()) sendWarning = true;

                    messages = messages.Where(x => x.CreatedAt > DateTime.Now.AddDays(-14));

                    messages.OrderBy(x => x.CreatedAt);
                    messages = messages.Take(amount);

                    await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messages);

                    RestUserMessage message;
                    if (sendWarning) message = await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"{messages.Count() - 1} messages pruned", "I can't delete messages over 2 weeks old"));
                    else message = await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"{messages.Count() - 1} messages pruned"));

                    await Task.Delay(5000);

                    await message.DeleteAsync();
                }
            }
        }

        [Command("Prune"), Alias("Purge", "Clear")]
        public async Task Prune(int amount, IUser user)
        {
            if (MessagePermission(Context.User, Context.Channel, Context.Channel))
            {
                if (BotHasPermissions(Context.Channel as ITextChannel, new[] { ChannelPermission.ManageMessages }, Context.Channel))
                {
                    await Context.Message.DeleteAsync();

                    await Task.Delay(1000); //Wait for message to delete in order to be accurate

                    bool sendWarning = false;

                    IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(amount * 1000).FlattenAsync();
                    messages = messages.Where(x => !x.IsPinned);
                    messages = messages.Where(x => x.Author.Id == user.Id);

                    if (messages.Where(x => x.CreatedAt > DateTime.Now.AddDays(-14)).Count() != messages.Count()) sendWarning = true;

                    messages = messages.Where(x => x.CreatedAt > DateTime.Now.AddDays(-14));

                    messages.OrderBy(x => x.CreatedAt);
                    messages = messages.Take(amount);

                    await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messages);

                    RestUserMessage message;
                    if (sendWarning) message = await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"{messages.Count()} messages pruned", $"Deleted messages sent by {user.Mention}\nI can't delete messages over 2 weeks old"));
                    else message = await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"{messages.Count()} messages pruned", $"Deleted messages sent by {user.Mention}"));

                    await Task.Delay(5000);

                    await message.DeleteAsync();
                }
            }
        }

        [Command("Prune"), Alias("Purge", "Clear")]
        public async Task Prune(int amount, string thing, int skip)
        {
            if (thing.ToLower() != "skip")
            {
                string prefix = ".";
                try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
                return;
            }

            await Context.Message.DeleteAsync();
            await Task.Delay(500);

            if (MessagePermission(Context.User, Context.Channel, Context.Channel))
            {
                if (BotHasPermissions(Context.Channel as ITextChannel, new[] { ChannelPermission.ManageMessages }, Context.Channel))
                {
                    bool sendWarning = false;

                    IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync((amount + skip) * 1000).FlattenAsync();
                    messages = messages.Where(x => !x.IsPinned);

                    if (messages.Where(x => x.CreatedAt > DateTime.Now.AddDays(-14)).Count() != messages.Count()) sendWarning = true;

                    messages = messages.Where(x => x.CreatedAt > DateTime.Now.AddDays(-14));

                    messages.OrderBy(x => x.CreatedAt);
                    messages = messages.Skip(skip).Take(amount);

                    await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messages);

                    RestUserMessage message;
                    if (sendWarning) message = await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"{messages.Count() - 1} messages pruned", "I can't delete messages over 2 weeks old"));
                    else message = await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"{messages.Count()} messages pruned"));

                    await Task.Delay(5000);

                    await message.DeleteAsync();
                }
            }
        }

        [Command("Prune"), Alias("Purge", "Clear")]
        public async Task Prune(int amount, ulong userId)
        {
            if (MessagePermission(Context.User, Context.Channel, Context.Channel))
            {
                if (BotHasPermissions(Context.Channel as ITextChannel, new[] { ChannelPermission.ManageMessages }, Context.Channel))
                {
                    await Context.Message.DeleteAsync();

                    bool sendWarning = false;

                    await Task.Delay(1000);

                    IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(amount * 1000).FlattenAsync();
                    messages = messages.Where(x => !x.IsPinned);
                    messages = messages.Where(x => x.Author.Id == userId);

                    if (messages.Where(x => x.CreatedAt > DateTime.Now.AddDays(-14)).Count() != messages.Count()) sendWarning = true;

                    messages = messages.Where(x => x.CreatedAt > DateTime.Now.AddDays(-14));

                    messages.OrderBy(x => x.CreatedAt);
                    messages = messages.Take(amount);

                    await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messages);

                    RestUserMessage message;
                    if (sendWarning) message = await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"{messages.Count()} messages pruned", $"Deleted messages sent by user with ID {userId}\nI can't delete messages over 2 weeks old"));
                    else message = await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"{messages.Count()} messages pruned", $"Deleted messages sent by user with ID {userId}"));

                    await Task.Delay(5000);

                    await message.DeleteAsync();
                }
            }
        }

        #endregion Prune

        #region JoinRole

        public static string JoinRoleHelpContent = "joinrole [role | none] - This role is added to users when they join the server";

        [Command("JoinRole")]
        public async Task JoinRole()
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(Context.Channel as ITextChannel, new[] { ChannelPermission.ManageRoles }, Context.Channel))
                {
                    await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Join Role Command", JoinRoleHelpContent));
                }
            }
        }

        [Command("JoinRole")]
        public async Task JoinRole(IRole role)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(Context.Channel as ITextChannel, new[] { ChannelPermission.ManageRoles }, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "JoinRole");
                    SaveData(Context.Guild.Id.ToString(), "JoinRole", role.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set join role", $"New users will be given role {role.Mention}"));
                }
            }
        }

        [Command("JoinRole")]
        public async Task JoinRole(string none)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (none.ToLower() == "none")
                {
                    DeleteData(Context.Guild.Id.ToString(), "JoinRole");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Removed join role", "New users will not be given a role"));
                }
                else
                {
                    string prefix = ".";
                    try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
                }
            }
        }

        #endregion JoinRole

        #region React

        public static string ReactHelpContent = "react [message id] [emote] - Add a reaction to a message in the current channel\n" +
            "react [channel] [message id] [emote] - Add a reaction to a message in another channel\n\n" +
            "Get a message ID by enabling developer mode in your client and right clicking the message";

        [Command("React"), Alias("Reaction", "AddReaction")]
        public async Task React()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("React Command", ReactHelpContent));
        }

        [Command("React")]
        public async Task React(ISocketMessageChannel channel, ulong messageId, [Remainder] string reactionString)
        {
            IUserMessage message = null;
            bool success = true;
            try
            {
                message = await channel.GetMessageAsync(messageId) as IUserMessage;
            }
            catch
            {
                success = false;
            }

            if (!success || message == null)
            {
                string prefix = ".";
                try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
                return;
            }

            if (MessagePermission(Context.User, channel, Context.Channel))
            {
                if (BotHasPermissions(message.Channel as ITextChannel, new[] { ChannelPermission.AddReactions }, Context.Channel))
                {
                    IEmote emote;
                    if (GetGuildEmote(reactionString, Context.Guild) != null) emote = GetGuildEmote(reactionString, Context.Guild);
                    else emote = GetDiscordEmote(reactionString);

                    Task task = message.AddReactionAsync(emote);

                    while (!task.IsCompleted) await Task.Delay(20);
                    if (task.IsCompletedSuccessfully) await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Added reaction", $"The {emote} reaction was added to a message in {(message.Channel as SocketTextChannel).Mention}"));
                    else await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid emote", "Use the actual emote in your command\n[Support Discord](https://discord.gg/WsxqABZ)"));
                }
            }
        }

        [Command("React")]
        public async Task React(ulong messageId, [Remainder] string reactionString)
        {
            ISocketMessageChannel channel = Context.Channel;

            IUserMessage message = null;
            bool success = true;
            try
            {
                message = await channel.GetMessageAsync(messageId) as IUserMessage;
            }
            catch
            {
                success = false;
            }

            if (!success || message == null)
            {
                string prefix = ".";
                try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
                return;
            }

            if (MessagePermission(Context.User, channel, Context.Channel))
            {
                if (BotHasPermissions(message.Channel as ITextChannel, new[] { ChannelPermission.AddReactions }, Context.Channel))
                {
                    IEmote emote;
                    if (GetGuildEmote(reactionString, Context.Guild) != null) emote = GetGuildEmote(reactionString, Context.Guild);
                    else emote = GetDiscordEmote(reactionString);

                    Task task = message.AddReactionAsync(emote);

                    while (!task.IsCompleted) await Task.Delay(20);
                    if (task.IsCompletedSuccessfully) await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Added reaction", $"The {emote} reaction was added to a message in {(message.Channel as SocketTextChannel).Mention}"));
                    else await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid emote", "Use the actual emote in your command\n[Support Discord](https://discord.gg/WsxqABZ)"));
                }
            }
        }

        #endregion React

        #region WhoHas

        [Command("WhoHas")]
        public async Task WhoHas(IRole role, int page = 1)
        {
            List<SocketGuildUser> users = Context.Guild.Users.Where(x => x.Roles.Where(b => b.Id == role.Id).Count() == 1).OrderBy(x => x.JoinedAt).ToList();
            int totalPages = int.Parse(Math.Ceiling(decimal.Parse(users.Count.ToString()) / decimal.Parse("50")).ToString());
            users = users.Skip((page - 1) * 50).Take(50).ToList();

            if ((page < 1 || page > totalPages) && totalPages != 0) { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid page")); return; }

            string output = "";
            foreach (SocketGuildUser user in users) output += $"{user.Mention}\n";
            if (output == "") output = "There are no users with that role.";

            if (totalPages == 0) page = 0;
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed($"Users with @{role.Name}", output, $"Page {page} of {totalPages}"));
        }

        #endregion WhoHas
    }
}