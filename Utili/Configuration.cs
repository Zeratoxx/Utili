using System.IO;
using System.Text.Json;

namespace Utili
{
    internal class Json
    {
        public static Configuration Config;

        public static void GenerateNewConfig()
        {
            Config = new Configuration();

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json;
            json = JsonSerializer.Serialize(Config, options);
            File.WriteAllText("Config.json", json);
        }

        public static void SaveConfig()
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(Config, options);
            File.WriteAllText("Config.json", json);
        }

        public static bool LoadConfig()
        {
            try
            {
                string json = File.ReadAllText("Config.json");
                Config = JsonSerializer.Deserialize<Configuration>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    internal class Configuration
    {
        public string Token { get; set; } = "";
        public string TestToken { get; set; } = "";
        public OtherBotTokens OtherBotTokens { get; set; } = new OtherBotTokens();
        public DatabaseInfo Database { get; set; } = new DatabaseInfo();
        public EmailLogin EmailInfo { get; set; } = new EmailLogin();
        public YoutubeInfo Youtube { get; set; } = new YoutubeInfo();
        public string Topgg { get; set; } = "";
        public string DiscordBots { get; set; } = "";
        public string BotsForDiscord { get; set; } = "";
        public string BotsOnDiscord { get; set; } = "";
        public string DiscordBoats { get; set; } = "";
        public string DiscordBotList { get; set; } = "";
        public string BotlistSpace { get; set; } = "";
    }

    internal class DatabaseInfo
    {
        public string Server { get; set; } = "";
        public string Database { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    internal class YoutubeInfo
    {
        public string ApplicationName { get; set; } = "";
        public string Key { get; set; } = "";
    }

    internal class OtherBotTokens
    {
        public string FileBot { get; set; } = "";
        public string HubBot { get; set; } = "";
        public string ThoriumCube { get; set; } = "";
        public string Unwyre { get; set; } = "";
        public string PingPlus { get; set; } = "";
        public string ImgOnly { get; set; } = "";
        public string Shards { get; set; } = "";
    }

    internal class EmailLogin
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}