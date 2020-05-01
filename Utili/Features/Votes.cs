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
    class Votes
    {
        public async Task Votes_MessageReceived(SocketMessage MessageParam)
        {
            var Message = MessageParam as SocketUserMessage;
            var Context = new SocketCommandContext(Program.Client, Message);

            if (Context.User.Id == Program.Client.CurrentUser.Id) return;

            if (GetData(Context.Guild.Id.ToString(), "Votes-Channel", Context.Channel.Id.ToString()).Count > 0)
            {
                bool React = false;
                if (GetData(Context.Guild.Id.ToString(), "Votes-Mode", "attachments").Count > 0)
                {
                    if (Message.Attachments.Count > 0 || Message.Content.Contains("youtube.com/watch?v=") || Message.Content.Contains("discordapp.com/attachment") || Message.Content.Contains("youtu.be/")) React = true;
                }
                else React = true;

                if (React)
                {
                    string UpName = "";
                    string DownName = "";
                    try { UpName = GetData(Context.Guild.Id.ToString(), "Votes-UpName").First().Value; } catch { }
                    try { DownName = GetData(Context.Guild.Id.ToString(), "Votes-DownName").First().Value; } catch { }

                    IEmote Emote;
                    if (GetGuildEmote(UpName, Context.Guild) != null) Emote = GetGuildEmote(UpName, Context.Guild);
                    else Emote = GetDiscordEmote(Base64Decode(UpName));
                    Task Task = Message.AddReactionAsync(Emote);

                    while (!Task.IsCompleted) await Task.Delay(20);
                    if(!Task.IsCompletedSuccessfully) Emote = GetDiscordEmote("⬆️");
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

            if (GetData(Channel.Guild.Id.ToString(), "Votes-Channel", Channel.Id.ToString()).Count == 0) return;

            if (Reaction.User.Value.IsBot) return;

            string UpName = "";
            string DownName = "";
            try { UpName = GetData(Channel.Guild.Id.ToString(), "Votes-UpName").First().Value; } catch { }
            try { DownName = GetData(Channel.Guild.Id.ToString(), "Votes-DownName").First().Value; } catch { }

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
            foreach(var RUsr in await Message.GetReactionUsersAsync(UpEmote, 99999).ToListAsync()) if (RUsr.Where(x => x.Id == Reaction.User.Value.Id).Count() > 0) ReactedUp = true;

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
                "upEmote [emote | reset] - Set the upvote emote\n" +
                "downEmote [emote | reset] - Set the downvote emote\n" +
                "mode [all | attachments] - Select which messages get emotes added\n" +
                "on [channel] - Enable voting in a channel\n" +
                "off [channel] - Disable voting in a channel";

        [Command("Help")]
        public async Task Help()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Votes", HelpContent, $"Prefix these commands with {Prefix}votes"));
        }

        [Command("")]
        public async Task Empty()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Votes", HelpContent, $"Prefix these commands with {Prefix}votes"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Votes", "In channels where this feature is enabled, messages with an attachment or a youtube link will be reacted to with upvote and downvote buttons which can be customised."));
        }

        [Command("upEmote")]
        public async Task upEmote(string EmoteName)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if(EmoteName.ToLower() == "reset")
                {
                    DeleteData(Context.Guild.Id.ToString(), "Votes-UpName");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Reset upvote emote", $"Upvote emote reset to ⬆️"));
                }
                else
                {
                    IEmote Emote = null;
                    bool Guild = false;
                    if (GetGuildEmote(EmoteName, Context.Guild) != null) { Emote = GetGuildEmote(EmoteName, Context.Guild); Guild = true; }
                    else Emote = GetDiscordEmote(EmoteName);

                    DeleteData(Context.Guild.Id.ToString(), "Votes-UpName");
                    if(Guild) SaveData(Context.Guild.Id.ToString(), "Votes-UpName", EmoteName);
                    else SaveData(Context.Guild.Id.ToString(), "Votes-UpName", Base64Encode(EmoteName));
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set upvote emote", $"Upvote emote set to {Emote}\n\n**Note:** If the emote does not display properly in this message, it's not working. Use the actual emote in your command or specify reset to go back to the defaul emote."));
                }
            }
        }

        [Command("downEmote")]
        public async Task downEmote(string EmoteName)
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (EmoteName.ToLower() == "reset")
                {
                    DeleteData(Context.Guild.Id.ToString(), "Votes-DownName");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Reset downvote emote", $"Downvote emote reset to ⬇️"));
                }
                else
                {
                    bool Guild = false;
                    IEmote Emote = null;
                    if (GetGuildEmote(EmoteName, Context.Guild) != null) { Emote = GetGuildEmote(EmoteName, Context.Guild); Guild = true; }
                    else Emote = GetDiscordEmote(EmoteName);

                    DeleteData(Context.Guild.Id.ToString(), "Votes-DownName");
                    if (Guild) SaveData(Context.Guild.Id.ToString(), "Votes-DownName", EmoteName);
                    else SaveData(Context.Guild.Id.ToString(), "Votes-DownName", Base64Encode(EmoteName));
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Set downvote emote", $"Downvote emote set to {Emote}\n\n**Note:** If the emote does not display properly in this message, it's not working. Use the actual emote in your command or specify reset to go back to the defaul emote."));
                }
            }
        }

        [Command("Mode")]
        public async Task Mode(string Mode)
        {
            if (Permission(Context.User, Context.Channel))
            {
                switch (Mode.ToLower())
                {
                    case "all":
                        DeleteData(Context.Guild.Id.ToString(), "Votes-Mode");
                        SaveData(Context.Guild.Id.ToString(), "Votes-Mode", "All");
                        await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Mode set", "All messages will be reacted to"));
                        break;
                    case "attachments":
                        DeleteData(Context.Guild.Id.ToString(), "Votes-Mode");
                        SaveData(Context.Guild.Id.ToString(), "Votes-Mode", "Attachments");
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
                if(BotHasPermissions(Channel, new ChannelPermission[] { ChannelPermission.ViewChannel, ChannelPermission.AddReactions }, Context.Channel))
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
