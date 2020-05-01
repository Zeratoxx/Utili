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
            Config = new Configuration
            {
                Token = "",
                TestToken = "",
                ShardsToken = "",
                OtherBotTokens = new OtherBotTokens
                {
                    FileBot = "",
                    HubBot = "",
                    ThoriumCube = "",
                    Unwyre = "",
                    PingPlus = "",
                    ImgOnly = "",
                    Shards = ""
                },
                Database = new DatabaseInfo
                {
                    Server = "",
                    Database = "",
                    Username = "",
                    Password = ""
                },
                Youtube = new YoutubeInfo
                {
                    ApplicationName = "",
                    Key = ""
                },
                DiscordBotListKey = "",
                BotsForDiscordKey = ""
            };

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
        public string Token { get; set; }
        public string TestToken { get; set; }
        public string ShardsToken { get; set; }
        public OtherBotTokens OtherBotTokens { get; set; }
        public DatabaseInfo Database { get; set; }
        public YoutubeInfo Youtube { get; set; }
        public string DiscordBotListKey { get; set; }
        public string BotsForDiscordKey { get; set; }
    }

    class DatabaseInfo
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    class YoutubeInfo
    {
        public string ApplicationName { get; set; }
        public string Key { get; set; }
    }

    class OtherBotTokens
    {
        public string FileBot { get; set; }
        public string HubBot { get; set; }
        public string ThoriumCube { get; set; }
        public string Unwyre { get; set; }
        public string PingPlus { get; set; }
        public string ImgOnly { get; set; }
        public string Shards { get; set; }
    }
}
