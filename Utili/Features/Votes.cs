using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;

namespace Utili
{
    internal class Votes
    {
        public async Task Votes_MessageReceived(SocketMessage messageParam)
        {
            SocketUserMessage message = messageParam as SocketUserMessage;
            SocketCommandContext context = new SocketCommandContext(Program.Client, message);

            if (context.User.Id == Program.Client.CurrentUser.Id) return;

            if (DataExists(context.Guild.Id.ToString(), "Votes-Channel", context.Channel.Id.ToString()))
            {
                bool react = false;

                string mode = "All";
                try { mode = GetFirstData(context.Guild.Id.ToString(), "Votes-Mode").Value; } catch { }
                try { mode = GetFirstData(context.Guild.Id.ToString(), $"Votes-Mode-{context.Channel.Id}").Value; } catch { }

                if (mode == "Attachments")
                {
                    if (message.Attachments.Count > 0 || message.Content.Contains("youtube.com/watch?v=") || message.Content.Contains("discordapp.com/attachment") || message.Content.Contains("youtu.be/")) react = true;
                }
                else react = true;

                if (react)
                {
                    string upName = "";
                    string downName = "";
                    try { upName = GetFirstData(context.Guild.Id.ToString(), "Votes-UpName").Value; } catch { }
                    try { downName = GetFirstData(context.Guild.Id.ToString(), "Votes-DownName").Value; } catch { }

                    try { upName = GetFirstData(context.Guild.Id.ToString(), $"Votes-UpName-{context.Channel.Id}").Value; } catch { }
                    try { downName = GetFirstData(context.Guild.Id.ToString(), $"Votes-DownName-{context.Channel.Id}").Value; } catch { }

                    IEmote emote;
                    if (GetGuildEmote(upName, context.Guild) != null) emote = GetGuildEmote(upName, context.Guild);
                    else emote = GetDiscordEmote(Base64Decode(upName));
                    Task task = message.AddReactionAsync(emote);

                    while (!task.IsCompleted) await Task.Delay(20);
                    if (!task.IsCompletedSuccessfully) emote = GetDiscordEmote("⬆️");
                    await message.AddReactionAsync(emote);

                    if (GetGuildEmote(downName, context.Guild) != null) emote = GetGuildEmote(downName, context.Guild);
                    else emote = GetDiscordEmote(Base64Decode(downName));
                    task = message.AddReactionAsync(emote);

                    while (!task.IsCompleted) await Task.Delay(20);
                    if (!task.IsCompletedSuccessfully) emote = GetDiscordEmote("⬇️");
                    await message.AddReactionAsync(emote);
                }
            }
        }

        public async Task Votes_ReactionAdded(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel cnl, SocketReaction reaction)
        {
            IUserMessage message = await msg.GetOrDownloadAsync();
            SocketGuildChannel channel = cnl as SocketGuildChannel;

            if (!DataExists(channel.Guild.Id.ToString(), "Votes-Channel", channel.Id.ToString())) return;

            if (reaction.User.Value.IsBot) return;

            string upName = "";
            string downName = "";
            try { upName = GetFirstData(channel.Guild.Id.ToString(), "Votes-UpName").Value; } catch { }
            try { downName = GetFirstData(channel.Guild.Id.ToString(), "Votes-DownName").Value; } catch { }

            try { upName = GetFirstData(channel.Guild.Id.ToString(), $"Votes-UpName-{channel.Id}").Value; } catch { }
            try { downName = GetFirstData(channel.Guild.Id.ToString(), $"Votes-DownName-{channel.Id}").Value; } catch { }

            IEmote upEmote;
            if (GetGuildEmote(upName, channel.Guild) != null) upEmote = GetGuildEmote(upName, channel.Guild);
            else upEmote = GetDiscordEmote(Base64Decode(upName));
            if (message.Reactions.Count(x => x.Key.Name == upEmote.Name) == 0) upEmote = GetDiscordEmote("⬆️");

            IEmote downEmote;
            if (GetGuildEmote(downName, channel.Guild) != null) downEmote = GetGuildEmote(downName, channel.Guild);
            else downEmote = GetDiscordEmote(Base64Decode(downName));
            if (message.Reactions.Count(x => x.Key.Name == downEmote.Name) == 0) downEmote = GetDiscordEmote("⬇️");

            if (reaction.Emote.Name != upEmote.Name && reaction.Emote.Name != downEmote.Name) return;

            bool reactedUp = false;
            foreach (IReadOnlyCollection<IUser> rUsr in await message.GetReactionUsersAsync(upEmote, 99999).ToListAsync()) if (rUsr.Where(x => x.Id == reaction.User.Value.Id).Count() > 0) reactedUp = true;

            bool reactedDown = false;
            foreach (IReadOnlyCollection<IUser> rUsr in await message.GetReactionUsersAsync(downEmote, 99999).ToListAsync()) if (rUsr.Where(x => x.Id == reaction.User.Value.Id).Count() > 0) reactedDown = true;

            if (reactedUp && reactedDown) await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
        }
    }

    [Group("Votes"), Alias("Polls")]
    public class VotesCommands : ModuleBase<SocketCommandContext>
    {
        public static string HelpContent =
                "help - Show this list\n" +
                "about - Display feature information\n" +
                "upEmote [channel] [emote | reset] - Set the upvote emote for a channel\n" +
                "downEmote [channel] [emote | reset] - Set the downvote emote for a channel\n" +
                "mode [channel] [all | attachments] - Select which messages get emotes added to them in a channel\n" +
                "**Tip:** Why not use a filter in combination with votes mode all?\n" +
                "on [channel] - Enable voting in a channel\n" +
                "off [channel] - Disable voting in a channel";

        [Command("Help")]
        public async Task Help()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Votes", HelpContent, $"Prefix these commands with {prefix}votes"));
        }

        [Command("")]
        public async Task Empty()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Votes", HelpContent, $"Prefix these commands with {prefix}votes"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Votes", "In channels where this feature is enabled, messages with an attachment or a youtube link will be reacted to with upvote and downvote buttons which can be customised."));
        }

        [Command("upEmote")]
        public async Task UpEmote(ITextChannel channel, string emoteName)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (emoteName.ToLower() == "reset")
                {
                    DeleteData(Context.Guild.Id.ToString(), $"Votes-UpName-{channel.Id}");
                    DeleteData(Context.Guild.Id.ToString(), "Votes-UpName"); // Previously channel id was not specified so this is for backwards-compatibility.
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Reset upvote emote", $"The upvote emote for {channel.Mention} has been reset to ⬆️"));
                }
                else
                {
                    IEmote emote = null;
                    bool guild = false;
                    if (GetGuildEmote(emoteName, Context.Guild) != null) { emote = GetGuildEmote(emoteName, Context.Guild); guild = true; }
                    else emote = GetDiscordEmote(emoteName);

                    DeleteData(Context.Guild.Id.ToString(), $"Votes-UpName-{channel.Id}");
                    if (guild) SaveData(Context.Guild.Id.ToString(), $"Votes-UpName-{channel.Id}", emoteName);
                    else SaveData(Context.Guild.Id.ToString(), $"Votes-UpName-{channel.Id}", Base64Encode(emoteName));
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set upvote emote", $"The upvote emote for {channel.Mention} has been set to {emote}\n\n**Note:** If the emote does not display properly in this message, it's not working. Use the actual emote in your command or specify reset to go back to the defaul emote."));
                }
            }
        }

        [Command("downEmote")]
        public async Task DownEmote(ITextChannel channel, string emoteName)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (emoteName.ToLower() == "reset")
                {
                    DeleteData(Context.Guild.Id.ToString(), $"Votes-DownName-{channel.Id}");
                    DeleteData(Context.Guild.Id.ToString(), "Votes-DownName"); // Previously channel id was not specified so this is for backwards-compatibility.
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Reset downvote emote", $"The downvote emote for {channel.Mention} has been reset to ⬇️"));
                }
                else
                {
                    bool guild = false;
                    IEmote emote = null;
                    if (GetGuildEmote(emoteName, Context.Guild) != null) { emote = GetGuildEmote(emoteName, Context.Guild); guild = true; }
                    else emote = GetDiscordEmote(emoteName);

                    DeleteData(Context.Guild.Id.ToString(), $"Votes-DownName-{channel.Id}");
                    if (guild) SaveData(Context.Guild.Id.ToString(), $"Votes-DownName-{channel.Id}", emoteName);
                    else SaveData(Context.Guild.Id.ToString(), $"Votes-DownName-{channel.Id}", Base64Encode(emoteName));
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set downvote emote", $"The downvote emote for {channel.Mention} has been set to {emote}\n\n**Note:** If the emote does not display properly in this message, it's not working. Use the actual emote in your command or specify reset to go back to the defaul emote."));
                }
            }
        }

        [Command("Mode")]
        public async Task Mode(ITextChannel channel, string mode)
        {
            if (Permission(Context.User, Context.Channel))
            {
                switch (mode.ToLower())
                {
                    case "all":
                        // Backwards compatibility not needed here (Keep DB entries with no channel specified) because unlike emotes there is no case for no value set.
                        DeleteData(Context.Guild.Id.ToString(), $"Votes-Mode-{channel.Id}");
                        SaveData(Context.Guild.Id.ToString(), $"Votes-Mode-{channel.Id}", "All");
                        await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Mode set", "All messages will be reacted to"));
                        break;

                    case "attachments":
                        DeleteData(Context.Guild.Id.ToString(), $"Votes-Mode-{channel.Id}");
                        SaveData(Context.Guild.Id.ToString(), $"Votes-Mode-{channel.Id}", "Attachments");
                        await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Mode set", "Only messages with attachments or youtube links will be reacted to"));
                        break;

                    default:
                        await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid mode", "all - React to all messages\nattachments - React to messages with attachments or youtube links"));
                        break;
                }
            }
        }

        [Command("On"), Alias("Enable")]
        public async Task On(ITextChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(channel, new[] { ChannelPermission.ViewChannel, ChannelPermission.AddReactions }, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "Votes-Channel", channel.Id.ToString());
                    SaveData(Context.Guild.Id.ToString(), "Votes-Channel", channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Enabled voting", $"Voting enabled in {channel.Mention}"));
                }
            }
        }

        [Command("Off"), Alias("Disable")]
        public async Task Off(ITextChannel channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "Votes-Channel", channel.Id.ToString());
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Disabled voting", $"Voting disabled in {channel.Mention}"));
            }
        }
    }
}