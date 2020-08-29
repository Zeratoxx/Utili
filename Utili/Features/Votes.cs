using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;
using static Utili.Data;
using static Utili.Logic;
using static Utili.SendMessage;

namespace Utili
{
    internal class Votes
    {
        public async Task Votes_MessageReceived(SocketMessage MessageParam)
        {
            var Message = MessageParam as SocketUserMessage;
            var Context = new SocketCommandContext(Program.Client, Message);

            if (Context.User.Id == Program.Client.CurrentUser.Id) return;

            if (DataExists(Context.Guild.Id.ToString(), "Votes-Channel", Context.Channel.Id.ToString()))
            {
                bool React = false;

                string Mode = "All";
                try { Mode = GetFirstData(Context.Guild.Id.ToString(), "Votes-Mode").Value; } catch { }
                try { Mode = GetFirstData(Context.Guild.Id.ToString(), $"Votes-Mode-{Context.Channel.Id}").Value; } catch { }

                if (Mode == "Attachments")
                {
                    if (Message.Attachments.Count > 0 || Message.Content.Contains("youtube.com/watch?v=") || Message.Content.Contains("discordapp.com/attachment") || Message.Content.Contains("youtu.be/")) React = true;
                }
                else React = true;

                if (React)
                {
                    string UpName = "";
                    string DownName = "";
                    try { UpName = GetFirstData(Context.Guild.Id.ToString(), "Votes-UpName").Value; } catch { }
                    try { DownName = GetFirstData(Context.Guild.Id.ToString(), "Votes-DownName").Value; } catch { }

                    try { UpName = GetFirstData(Context.Guild.Id.ToString(), $"Votes-UpName-{Context.Channel.Id}").Value; } catch { }
                    try { DownName = GetFirstData(Context.Guild.Id.ToString(), $"Votes-DownName-{Context.Channel.Id}").Value; } catch { }

                    IEmote Emote;
                    if (GetGuildEmote(UpName, Context.Guild) != null) Emote = GetGuildEmote(UpName, Context.Guild);
                    else Emote = GetDiscordEmote(Base64Decode(UpName));
                    Task Task = Message.AddReactionAsync(Emote);

                    while (!Task.IsCompleted) await Task.Delay(20);
                    if (!Task.IsCompletedSuccessfully) Emote = GetDiscordEmote("⬆️");
                    await Message.AddReactionAsync(Emote);

                    if (GetGuildEmote(DownName, Context.Guild) != null) Emote = GetGuildEmote(DownName, Context.Guild);
                    else Emote = GetDiscordEmote(Base64Decode(DownName));
                    Task = Message.AddReactionAsync(Emote);

                    while (!Task.IsCompleted) await Task.Delay(20);
                    if (!Task.IsCompletedSuccessfully) Emote = GetDiscordEmote("⬇️");
                    await Message.AddReactionAsync(Emote);
                }
            }
        }

        public async Task Votes_ReactionAdded(Cacheable<IUserMessage, ulong> Msg, ISocketMessageChannel Cnl, SocketReaction Reaction)
        {
            var Message = await Msg.GetOrDownloadAsync();
            var Channel = Cnl as SocketGuildChannel;

            if (!DataExists(Channel.Guild.Id.ToString(), "Votes-Channel", Channel.Id.ToString())) return;

            if (Reaction.User.Value.IsBot) return;

            string UpName = "";
            string DownName = "";
            try { UpName = GetFirstData(Channel.Guild.Id.ToString(), "Votes-UpName").Value; } catch { }
            try { DownName = GetFirstData(Channel.Guild.Id.ToString(), "Votes-DownName").Value; } catch { }

            try { UpName = GetFirstData(Channel.Guild.Id.ToString(), $"Votes-UpName-{Channel.Id}").Value; } catch { }
            try { DownName = GetFirstData(Channel.Guild.Id.ToString(), $"Votes-DownName-{Channel.Id}").Value; } catch { }

            IEmote UpEmote;
            if (GetGuildEmote(UpName, Channel.Guild) != null) UpEmote = GetGuildEmote(UpName, Channel.Guild);
            else UpEmote = GetDiscordEmote(Base64Decode(UpName));
            if (Message.Reactions.Count(x => x.Key.Name == UpEmote.Name) == 0) UpEmote = GetDiscordEmote("⬆️");

            IEmote DownEmote;
            if (GetGuildEmote(DownName, Channel.Guild) != null) DownEmote = GetGuildEmote(DownName, Channel.Guild);
            else DownEmote = GetDiscordEmote(Base64Decode(DownName));
            if (Message.Reactions.Count(x => x.Key.Name == DownEmote.Name) == 0) DownEmote = GetDiscordEmote("⬇️");

            if (Reaction.Emote.Name != UpEmote.Name && Reaction.Emote.Name != DownEmote.Name) return;

            bool ReactedUp = false;
            foreach (var RUsr in await Message.GetReactionUsersAsync(UpEmote, 99999).ToListAsync()) if (RUsr.Where(x => x.Id == Reaction.User.Value.Id).Count() > 0) ReactedUp = true;

            bool ReactedDown = false;
            foreach (var RUsr in await Message.GetReactionUsersAsync(DownEmote, 99999).ToListAsync()) if (RUsr.Where(x => x.Id == Reaction.User.Value.Id).Count() > 0) ReactedDown = true;

            if (ReactedUp && ReactedDown) await Message.RemoveReactionAsync(Reaction.Emote, Reaction.User.Value);
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
            string Prefix = ".";
            try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Votes", HelpContent, $"Prefix these commands with {Prefix}votes"));
        }

        [Command("")]
        public async Task Empty()
        {
            string Prefix = ".";
            try { Prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Votes", HelpContent, $"Prefix these commands with {Prefix}votes"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Votes", "In channels where this feature is enabled, messages with an attachment or a youtube link will be reacted to with upvote and downvote buttons which can be customised."));
        }

        [Command("upEmote")]
        public async Task upEmote(ITextChannel Channel, string EmoteName)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (EmoteName.ToLower() == "reset")
                {
                    DeleteData(Context.Guild.Id.ToString(), $"Votes-UpName-{Channel.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Votes-UpName"); // Previously channel id was not specified so this is for backwards-compatibility.
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Reset upvote emote", $"The upvote emote for {Channel.Mention} has been reset to ⬆️"));
                }
                else
                {
                    IEmote Emote = null;
                    bool Guild = false;
                    if (GetGuildEmote(EmoteName, Context.Guild) != null) { Emote = GetGuildEmote(EmoteName, Context.Guild); Guild = true; }
                    else Emote = GetDiscordEmote(EmoteName);

                    DeleteData(Context.Guild.Id.ToString(), $"Votes-UpName-{Channel.Id}");
                    if (Guild) SaveData(Context.Guild.Id.ToString(), $"Votes-UpName-{Channel.Id}", EmoteName);
                    else SaveData(Context.Guild.Id.ToString(), $"Votes-UpName-{Channel.Id}", Base64Encode(EmoteName));
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set upvote emote", $"The upvote emote for {Channel.Mention} has been set to {Emote}\n\n**Note:** If the emote does not display properly in this message, it's not working. Use the actual emote in your command or specify reset to go back to the defaul emote."));
                }
            }
        }

        [Command("downEmote")]
        public async Task downEmote(ITextChannel Channel, string EmoteName)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (EmoteName.ToLower() == "reset")
                {
                    DeleteData(Context.Guild.Id.ToString(), $"Votes-DownName-{Channel.Id}");
                    DeleteData(Context.Guild.Id.ToString(), $"Votes-DownName"); // Previously channel id was not specified so this is for backwards-compatibility.
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Reset downvote emote", $"The downvote emote for {Channel.Mention} has been reset to ⬇️"));
                }
                else
                {
                    bool Guild = false;
                    IEmote Emote = null;
                    if (GetGuildEmote(EmoteName, Context.Guild) != null) { Emote = GetGuildEmote(EmoteName, Context.Guild); Guild = true; }
                    else Emote = GetDiscordEmote(EmoteName);

                    DeleteData(Context.Guild.Id.ToString(), $"Votes-DownName-{Channel.Id}");
                    if (Guild) SaveData(Context.Guild.Id.ToString(), $"Votes-DownName-{Channel.Id}", EmoteName);
                    else SaveData(Context.Guild.Id.ToString(), $"Votes-DownName-{Channel.Id}", Base64Encode(EmoteName));
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set downvote emote", $"The downvote emote for {Channel.Mention} has been set to {Emote}\n\n**Note:** If the emote does not display properly in this message, it's not working. Use the actual emote in your command or specify reset to go back to the defaul emote."));
                }
            }
        }

        [Command("Mode")]
        public async Task Mode(ITextChannel Channel, string Mode)
        {
            if (Permission(Context.User, Context.Channel))
            {
                switch (Mode.ToLower())
                {
                    case "all":
                        // Backwards compatibility not needed here (Keep DB entries with no channel specified) because unlike emotes there is no case for no value set.
                        DeleteData(Context.Guild.Id.ToString(), $"Votes-Mode-{Channel.Id}");
                        SaveData(Context.Guild.Id.ToString(), $"Votes-Mode-{Channel.Id}", "All");
                        await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Mode set", "All messages will be reacted to"));
                        break;

                    case "attachments":
                        DeleteData(Context.Guild.Id.ToString(), $"Votes-Mode-{Channel.Id}");
                        SaveData(Context.Guild.Id.ToString(), $"Votes-Mode-{Channel.Id}", "Attachments");
                        await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Mode set", "Only messages with attachments or youtube links will be reacted to"));
                        break;

                    default:
                        await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Invalid mode", "all - React to all messages\nattachments - React to messages with attachments or youtube links"));
                        break;
                }
            }
        }

        [Command("On"), Alias("Enable")]
        public async Task On(ITextChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(Channel, new ChannelPermission[] { ChannelPermission.ViewChannel, ChannelPermission.AddReactions }, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "Votes-Channel", Channel.Id.ToString());
                    SaveData(Context.Guild.Id.ToString(), "Votes-Channel", Channel.Id.ToString());
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Enabled voting", $"Voting enabled in {Channel.Mention}"));
                }
            }
        }

        [Command("Off"), Alias("Disable")]
        public async Task Off(ITextChannel Channel)
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "Votes-Channel", Channel.Id.ToString());
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Disabled voting", $"Voting disabled in {Channel.Mention}"));
            }
        }
    }
}