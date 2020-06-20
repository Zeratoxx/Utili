using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Utili
{
    class Json
    {
        public static Configuration Config;

        public static void GenerateNewConfig()
        {
            Config = new Configuration();

            JsonSerializerOptions Options = new JsonSerializerOptions();
            Options.WriteIndented = true;

            string Json;
            Json = JsonSerializer.Serialize(Config, Options);
            File.WriteAllText("Config.json", Json);
        }

        public static void SaveConfig()
        {
            JsonSerializerOptions Options = new JsonSerializerOptions();
            Options.WriteIndented = true;

            string Json = JsonSerializer.Serialize(Config, Options);
            File.WriteAllText("Config.json", Json);
        }

        public static bool LoadConfig()
        {
            try
            {
                string Json = File.ReadAllText("Config.json");
                Config = JsonSerializer.Deserialize<Configuration>(Json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    class Configuration
    {
        public string Token { get; set; } = "";
        public string TestToken { get; set; } = "";
        public OtherBotTokens OtherBotTokens { get; set; } = new OtherBotTokens();
        public DatabaseInfo Database { get; set; } = new DatabaseInfo();
        public EmailLogin EmailInfo { get; set; } = new EmailLogin();
        public YoutubeInfo Youtube { get; set; } = new YoutubeInfo();
        public string DiscordBotListKey { get; set; } = "";
        public string BotsForDiscordKey { get; set; } = "";
        public string BotsOnDiscordKey { get; set; } = "";
        public string DiscordBoatsKey { get; set; } = "";
    }

    class DatabaseInfo
    {
        public string Server { get; set; } = "";
        public string Database { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    class YoutubeInfo
    {
        public string ApplicationName { get; set; } = "";
        public string Key { get; set; } = "";
    }

    class OtherBotTokens
    {
        public string FileBot { get; set; } = "";
        public string HubBot { get; set; } = "";
        public string ThoriumCube { get; set; } = "";
        public string Unwyre { get; set; } = "";
        public string PingPlus { get; set; } = "";
        public string ImgOnly { get; set; } = "";
        public string Shards { get; set; } = "";
    }

    class EmailLogin
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
