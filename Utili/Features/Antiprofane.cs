using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
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

        public async Task AntiProfane_MessageReceived(SocketMessage messageParam)
        {
            SocketUserMessage message = messageParam as SocketUserMessage;
            SocketCommandContext context = new SocketCommandContext(_client, message);

            if (DataExists(context.Guild.Id.ToString(), "AntiProfane-Enabled", "True"))
            {
                if (await IsProfaneAsync(context.Message.Content))
                {
                    if (!GetPerms(context.Channel).ManageMessages) return;

                    await context.Message.DeleteAsync();
                    if (context.User.Id != _client.CurrentUser.Id)
                    {
                        RestUserMessage sentMessage = await context.Channel.SendMessageAsync(embed: GetEmbed("No", "Message deleted", "This server doesn't allow profane langugae.\n[Report false positive](https://discord.gg/WsxqABZ)"));
                        Thread.Sleep(5000);
                        await sentMessage.DeleteAsync();
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

        private async Task<bool> IsProfaneAsync(string content)
        {
            List<string> detectedWords = new List<string>();
            string toTest = content;
            toTest = toTest.Replace(" ", "");
            toTest = toTest.Replace("_", "");
            toTest = toTest.Replace("-", "");
            toTest = toTest.Replace(".", "");
            toTest = toTest.Replace("~", "");
            toTest = toTest.Replace(",", "");
            foreach (KeyValuePair<string, string> x in LeetRules) toTest = toTest.Replace(x.Key, x.Value);
            toTest = toTest.ToLower();
            foreach (string word in BadWords) if (toTest.Contains(word)) if (!detectedWords.Contains(word)) detectedWords.Add(word);
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");

            toTest = content.Replace(" ", "").ToLower();
            toTest = rgx.Replace(toTest, "");
            foreach (string word in BadWords) if (toTest.Contains(word)) if (!detectedWords.Contains(word)) detectedWords.Add(word);

            List<string> iteration = detectedWords;

            try
            {
                foreach (string detectedWord in iteration)
                {
                    try
                    {
                        bool allow = false;
                        foreach (string intendedWord in content.ToLower().Split(" "))
                        {
                            if (intendedWord.Contains(detectedWord)) if (GoodWords.Contains(rgx.Replace(intendedWord, ""))) allow = true;
                        }
                        if (allow) try { detectedWords.Remove(detectedWord); } catch { }
                    }
                    catch { }
                }
            }
            catch { }

            return (detectedWords.Count != 0);
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
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Anti-Profane filter", HelpContent, $"Prefix these commands with {prefix}antiprofane"));
        }

        [Command("")]
        public async Task Empty()
        {
            string prefix = ".";
            try { prefix = GetFirstData(Context.Guild.Id.ToString(), "Prefix").Value; } catch { }
            await Context.Channel.SendMessageAsync(embed: GetLargeEmbed("Anti-Profane filter", HelpContent, $"Prefix these commands with {prefix}antiprofane"));
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
                if (BotHasPermissions(Context.Guild, new[] { GuildPermission.ViewChannel, GuildPermission.ManageMessages }, Context.Channel))
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