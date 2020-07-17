using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Utili.Data;
using static Utili.Logic;
using static Utili.Program;
using static Utili.SendMessage;

namespace Utili
{
    internal class AntiProfane
    {
        public static Dictionary<string, string> LeetRules = new Dictionary<string, string>();
        public static List<string> BadWords;
        public static List<string> GoodWords;

        public async Task AntiProfane_MessageReceived(SocketMessage MessageParam)
        {
            var Message = MessageParam as SocketUserMessage;
            var Context = new SocketCommandContext(Client, Message);

            if (GetData(Context.Guild.Id.ToString(), "AntiProfane-Enabled", "True").Count > 0)
            {
                if (await IsProfaneAsync(Context.Message.Content))
                {
                    await Context.Message.DeleteAsync();
                    if (Context.User.Id != Program.Client.CurrentUser.Id)
                    {
                        var SentMessage = await Context.Channel.SendMessageAsync(embed: GetEmbed("No", "Message deleted", "This server doesn't allow profane langugae.\n[Report false positive](https://discord.gg/WsxqABZ)"));
                        Thread.Sleep(5000);
                        await SentMessage.DeleteAsync();
                    }
                }
            }
        }

        public async Task AntiProfane_Ready()
        {
            if (!File.Exists("GoodWords.txt"))
            {
                StreamWriter sw = File.CreateText("GoodWords.txt");
                sw.WriteLine("https://raw.githubusercontent.com/first20hours/google-10000-english/master/google-10000-english-no-swears.txt");
                GoodWords = new List<string>();
                sw.Close();
            }
            else
            {
                StreamReader sr = new StreamReader("GoodWords.txt");
                string line = sr.ReadLine();
                GoodWords = new List<string>();
                while (line != null) if (line != "https://raw.githubusercontent.com/first20hours/google-10000-english/master/google-10000-english-no-swears.txt") { GoodWords.Add(line); line = sr.ReadLine(); }
                sr.Close();
            }

            if (!File.Exists("BadWords.txt"))
            {
                StreamWriter sw = File.CreateText("BadWords.txt");
                sw.WriteLine("https://github.com/LDNOOBW/List-of-Dirty-Naughty-Obscene-and-Otherwise-Bad-Words/blob/master/en");
                BadWords = new List<string>();
                sw.Close();
            }
            else
            {
                StreamReader sr = new StreamReader("BadWords.txt");
                string line = sr.ReadLine();
                BadWords = new List<string>();
                while (line != null) if (line != "https://github.com/LDNOOBW/List-of-Dirty-Naughty-Obscene-and-Otherwise-Bad-Words/blob/master/en") { BadWords.Add(line); line = sr.ReadLine(); }
                sr.Close();
            }
        }

        private async Task<bool> IsProfaneAsync(string Content)
        {
            List<string> DetectedWords = new List<string>();
            string ToTest = Content;
            ToTest = ToTest.Replace(" ", "");
            ToTest = ToTest.Replace("_", "");
            ToTest = ToTest.Replace("-", "");
            ToTest = ToTest.Replace(".", "");
            ToTest = ToTest.Replace("~", "");
            ToTest = ToTest.Replace(",", "");
            foreach (KeyValuePair<string, string> x in LeetRules) ToTest = ToTest.Replace(x.Key, x.Value);
            ToTest = ToTest.ToLower();
            foreach (string word in BadWords) if (ToTest.Contains(word)) if (!DetectedWords.Contains(word)) DetectedWords.Add(word);
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");

            ToTest = Content.Replace(" ", "").ToLower();
            ToTest = rgx.Replace(ToTest, "");
            foreach (string word in BadWords) if (ToTest.Contains(word)) if (!DetectedWords.Contains(word)) DetectedWords.Add(word);

            List<string> Iteration = DetectedWords;

            try
            {
                foreach (string DetectedWord in Iteration)
                {
                    try
                    {
                        bool allow = false;
                        foreach (string intendedWord in Content.ToLower().Split(" "))
                        {
                            if (intendedWord.Contains(DetectedWord)) if (GoodWords.Contains(rgx.Replace(intendedWord, ""))) allow = true;
                        }
                        if (allow) try { DetectedWords.Remove(DetectedWord); } catch { }
                    }
                    catch { }
                }
            }
            catch { };

            return (DetectedWords.Count != 0);
        }
    }

    [Group("Antiprofane")]
    public class AntiprofaneCommands : ModuleBase<SocketCommandContext>
    {
        public static string HelpContent =
                "help - Show this list\n" +
                "about - Display feature information\n" +
                "on - Enable the feature in the server\n" +
                "off - Disable the feature in the server";

        [Command("Help")]
        public async Task Help()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Anti-Profane filter", HelpContent, $"Prefix these commands with {Prefix}antiprofane"));
        }

        [Command("")]
        public async Task Empty()
        {
            string Prefix = ".";
            try { Prefix = Data.GetData(Context.Guild.Id.ToString(), "Prefix").First().Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Anti-Profane filter", HelpContent, $"Prefix these commands with {Prefix}antiprofane"));
        }

        [Command("About")]
        public async Task About()
        {
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Anti-Profane filter", "Enable this filter to delete messages containing profane language using a pre-configured algorithm.\n[Report false positive](https://discord.gg/WsxqABZ)"));
        }

        [Command("On"), Alias("Enable")]
        public async Task On()
        {
            if (Permission(Context.User, Context.Channel))
            {
                if (BotHasPermissions(Context.Guild, new GuildPermission[] { GuildPermission.ReadMessages, GuildPermission.ManageMessages }, Context.Channel))
                {
                    DeleteData(Context.Guild.Id.ToString(), "AntiProfane-Enabled");
                    SaveData(Context.Guild.Id.ToString(), "AntiProfane-Enabled", "True");
                    await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Anti-profane filter enabled"));
                }
            }
        }

        [Command("Off"), Alias("Disable")]
        public async Task Off()
        {
            if (Permission(Context.User, Context.Channel))
            {
                DeleteData(Context.Guild.Id.ToString(), "AntiProfane-Enabled");
                await Context.Channel.SendMessageAsync(embed: GetEmbed("Yes", "Anti-profane filter disabled"));
            }
        }
    }
}