using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task Prune(int Amount)
        {
            Amount += 1; //Account for the command message
            if (MessagePermission(Context.User, Context.Channel, Context.Channel))
            {
                if (BotHasPermissions(Context.Channel as ITextChannel, new ChannelPermission[] { ChannelPermission.ManageMessages }, Context.Channel))
                {
                    bool SendWarning = false;

                    var Messages = await Context.Channel.GetMessagesAsync(Amount * 1000).FlattenAsync();
                    Messages = Messages.Where(x => !x.IsPinned);

                    if (Messages.Where(x => x.CreatedAt > DateTime.Now.AddDays(-14)).Count() != Messages.Count()) SendWarning = true;

                    Messages = Messages.Where(x => x.CreatedAt > DateTime.Now.AddDays(-14));

                    Messages.OrderBy(x => x.CreatedAt);
                    Messages = Messages.Take(Amount);

                    await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(Messages);

                    RestUserMessage Message;
                    if (SendWarning) Message = await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"{Messages.Count() - 1} messages pruned", "I can't delete messages over 2 weeks old"));
                    else Message = await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"{Messages.Count() - 1} messages pruned"));

                    await Task.Delay(5000);

                    await Message.DeleteAsync();
                }
            }
        }

        [Command("Prune"), Alias("Purge", "Clear")]
        public async Task Prune(int Amount, IUser User)
        {
            if (MessagePermission(Context.User, Context.Channel, Context.Channel))
            {
                if (BotHasPermissions(Context.Channel as ITextChannel, new ChannelPermission[] { ChannelPermission.ManageMessages }, Context.Channel))
                {
                    await Context.Message.DeleteAsync();

                    await Task.Delay(1000); //Wait for message to delete in order to be accurate

                    bool SendWarning = false;

                    var Messages = await Context.Channel.GetMessagesAsync(Amount * 1000).FlattenAsync();
                    Messages = Messages.Where(x => !x.IsPinned);
                    Messages = Messages.Where(x => x.Author.Id == User.Id);

                    if (Messages.Where(x => x.CreatedAt > DateTime.Now.AddDays(-14)).Count() != Messages.Count()) SendWarning = true;

                    Messages = Messages.Where(x => x.CreatedAt > DateTime.Now.AddDays(-14));

                    Messages.OrderBy(x => x.CreatedAt);
                    Messages = Messages.Take(Amount);

                    await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(Messages);

                    RestUserMessage Message;
                    if (SendWarning) Message = await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"{Messages.Count()} messages pruned", $"Deleted messages sent by {User.Mention}\nI can't delete messages over 2 weeks old"));
                    else Message = await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"{Messages.Count()} messages pruned", $"Deleted messages sent by {User.Mention}"));

                    await Task.Delay(5000);

                    await Message.DeleteAsync();
                }
            }
        }

        [Command("Prune"), Alias("Purge", "Clear")]
        public async Task Prune(int Amount, string thing, int Skip)
        {
            if (thing.ToLower() != "skip")
            {
                string Prefix = ".";
                try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {Prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
                return;
            }

            await Context.Message.DeleteAsync();
            await Task.Delay(500);

            if (MessagePermission(Context.User, Context.Channel, Context.Channel))
            {
                if (BotHasPermissions(Context.Channel as ITextChannel, new ChannelPermission[] { ChannelPermission.ManageMessages }, Context.Channel))
                {
                    bool SendWarning = false;

                    var Messages = await Context.Channel.GetMessagesAsync((Amount + Skip) * 1000).FlattenAsync();
                    Messages = Messages.Where(x => !x.IsPinned);

                    if (Messages.Where(x => x.CreatedAt > DateTime.Now.AddDays(-14)).Count() != Messages.Count()) SendWarning = true;

                    Messages = Messages.Where(x => x.CreatedAt > DateTime.Now.AddDays(-14));

                    Messages.OrderBy(x => x.CreatedAt);
                    Messages = Messages.Skip(Skip).Take(Amount);

                    await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(Messages);

                    RestUserMessage Message;
                    if (SendWarning) Message = await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"{Messages.Count() - 1} messages pruned", "I can't delete messages over 2 weeks old"));
                    else Message = await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"{Messages.Count()} messages pruned"));

                    await Task.Delay(5000);

                    await Message.DeleteAsync();
                }
            }
        }

        [Command("Prune"), Alias("Purge", "Clear")]
        public async Task Prune(int Amount, ulong UserID)
        {
            if (MessagePermission(Context.User, Context.Channel, Context.Channel))
            {
                if (BotHasPermissions(Context.Channel as ITextChannel, new ChannelPermission[] { ChannelPermission.ManageMessages }, Context.Channel))
                {
                    await Context.Message.DeleteAsync();

                    bool SendWarning = false;

                    await Task.Delay(1000);

                    var Messages = await Context.Channel.GetMessagesAsync(Amount * 1000).FlattenAsync();
                    Messages = Messages.Where(x => !x.IsPinned);
                    Messages = Messages.Where(x => x.Author.Id == UserID);

                    if (Messages.Where(x => x.CreatedAt > DateTime.Now.AddDays(-14)).Count() != Messages.Count()) SendWarning = true;

                    Messages = Messages.Where(x => x.CreatedAt > DateTime.Now.AddDays(-14));

                    Messages.OrderBy(x => x.CreatedAt);
                    Messages = Messages.Take(Amount);

                    await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(Messages);

                    RestUserMessage Message;
                    if (SendWarning) Message = await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"{Messages.Count()} messages pruned", $"Deleted messages sent by user with ID {UserID}\nI can't delete messages over 2 weeks old"));
                    else Message = await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"{Messages.Count()} messages pruned", $"Deleted messages sent by user with ID {UserID}"));

                    await Task.Delay(5000);

                    await Message.DeleteAsync();
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
                if (BotHasPermissions(Context.Channel as ITextChannel, new ChannelPermission[] { ChannelPermission.ManageRoles }, Context.Channel))
                {
                    await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Join Role Command", JoinRoleHelpContent));
                }
            }
        }

        [Command("JoinRole")]
        public async Task JoinRole(IRole Role)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(Context.Channel as ITextChannel, new ChannelPermission[] { ChannelPermission.ManageRoles }, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "JoinRole");
                    SaveData(Context.Guild.Id.ToString(), "JoinRole", Role.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set join role", $"New users will be given role {Role.Mention}"));
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
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Removed join role", $"New users will not be given a role"));
                }
                else
                {
                    string Prefix = ".";
                    try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {Prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
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
        public async Task React(ISocketMessageChannel Channel, ulong MessageID, [Remainder] string ReactionString)
        {
            IUserMessage Message = null;
            bool Success = true;
            try
            {
                Message = await Channel.GetMessageAsync(MessageID) as IUserMessage;
            }
            catch
            {
                Success = false;
            }

            if (!Success || Message == null)
            {
                string Prefix = ".";
                try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {Prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
                return;
            }

            if (MessagePermission(Context.User, Channel, Context.Channel))
            {
                if (BotHasPermissions(Message.Channel as ITextChannel, new ChannelPermission[] { ChannelPermission.AddReactions }, Context.Channel))
                {
                    IEmote Emote = null;
                    if (GetGuildEmote(ReactionString, Context.Guild) != null) Emote = GetGuildEmote(ReactionString, Context.Guild);
                    else Emote = GetDiscordEmote(ReactionString);

                    Task Task = Message.AddReactionAsync(Emote);

                    while (!Task.IsCompleted) await Task.Delay(20);
                    if (Task.IsCompletedSuccessfully) await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"Added reaction", $"The {Emote} reaction was added to a message in {(Message.Channel as SocketTextChannel).Mention}"));
                    else await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid emote", "Use the actual emote in your command\n[Support Discord](https://discord.gg/WsxqABZ)"));
                }
            }
        }

        [Command("React")]
        public async Task React(ulong MessageID, [Remainder] string ReactionString)
        {
            ISocketMessageChannel Channel = Context.Channel;

            IUserMessage Message = null;
            bool Success = true;
            try
            {
                Message = await Channel.GetMessageAsync(MessageID) as IUserMessage;
            }
            catch
            {
                Success = false;
            }

            if (!Success || Message == null)
            {
                string Prefix = ".";
                try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
                await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid command syntax", $"Try {Prefix}help\n[Support Discord](https://discord.gg/WsxqABZ)"));
                return;
            }

            if (MessagePermission(Context.User, Channel, Context.Channel))
            {
                if (BotHasPermissions(Message.Channel as ITextChannel, new ChannelPermission[] { ChannelPermission.AddReactions }, Context.Channel))
                {
                    IEmote Emote = null;
                    if (GetGuildEmote(ReactionString, Context.Guild) != null) Emote = GetGuildEmote(ReactionString, Context.Guild);
                    else Emote = GetDiscordEmote(ReactionString);

                    Task Task = Message.AddReactionAsync(Emote);

                    while (!Task.IsCompleted) await Task.Delay(20);
                    if (Task.IsCompletedSuccessfully) await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", $"Added reaction", $"The {Emote} reaction was added to a message in {(Message.Channel as SocketTextChannel).Mention}"));
                    else await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid emote", "Use the actual emote in your command\n[Support Discord](https://discord.gg/WsxqABZ)"));
                }
            }
        }

        #endregion React

        #region WhoHas

        [Command("WhoHas")]
        public async Task WhoHas(IRole Role, int Page = 1)
        {
            var Users = Context.Guild.Users.Where(x => x.Roles.Where(b => b.Id == Role.Id).Count() == 1).OrderBy(x => x.JoinedAt).ToList();
            int TotalPages = int.Parse(Math.Ceiling(decimal.Parse(Users.Count.ToString()) / decimal.Parse("50")).ToString());
            Users = Users.Skip((Page - 1) * 50).Take(50).ToList();

            if ((Page < 1 || Page > TotalPages) && TotalPages != 0) { await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid page")); return; }

            string Output = "";
            foreach (var User in Users) Output += $"{User.Mention}\n";
            if (Output == "") Output = "There are no users with that role.";

            if (TotalPages == 0) Page = 0;
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed($"Users with @{Role.Name}", Output, $"Page {Page} of {TotalPages}"));
        }

        #endregion WhoHas
    }
}