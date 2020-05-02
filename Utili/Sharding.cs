using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Collections.Generic;

using static Utili.SendMessage;
using static Utili.Data;
using static Utili.Json;

using System.Threading;
using System.Linq;
using System.Runtime.InteropServices;
using DiscordBotsList.Api;
using System.Net.Http;
using System.Net;
using MySql.Data.MySqlClient;

namespace Utili
{
    class Sharding
    {
        public static async Task<int> GetShardID()
        {
            while (true)
            {
                try
                {
                    Random Random = new Random();

                    int Shards = await GetTotalShards();

                    while (true)
                    {
                        int Target = Random.Next(0, Shards);

                        if (GetShardData(Target, "Online").Count == 0 && GetShardData(Target, "Reserved").Count == 0)
                        {
                            SaveData(Target, "Reserved", DateTime.Now);
                            await Task.Delay(2500);

                            if (GetShardData(Target, "Reserved").Count == 1 && GetShardData(Target, "Online").Count == 0)
                            {
                                SaveData(Target, "Online", DateTime.Now);
                                DeleteData(Target, "Reserved");
                                return Target;
                            }
                            else DeleteData(Target, "Reserved");
                        }

                        await FlushDisconnected(false);
                    }
                }
                catch { }
            }
        }

        public static async Task<int> GetTotalShards()
        {
            return GetShardData(Type: "ShardCount").First().ShardID;
        }

        public static async Task KeepConnection()
        {
            while (true)
            {
                try
                {
                    SaveData(Program.Client.ShardId, "Online", DateTime.Now);

                    var ShardData = GetShardData(Program.Client.ShardId, "Online");
                    ShardData = ShardData.OrderBy(x => x.Heartbeat).ToList();
                    ShardData.Reverse();
                    ShardData.RemoveAt(0);

                    foreach (var OldData in ShardData)
                    {
                        DeleteData(OldData.ID);
                    }
                }
                catch { };

                await Task.Delay(10000);
            }
        }

        public static async Task UpdateShardMessage()
        {
            using (var Client = Program.ShardsClient)
            {
                while (true)
                {
                    try
                    {
                        string MessageContent = "";
                        int Online = 0;

                        for (int i = 0; i < await GetTotalShards(); i++)
                        {
                            if (GetShardData(i, "Online").Count > 0) { MessageContent += $"Shard {i + 1}: Online\n"; Online += 1; }
                            else MessageContent += $"Shard {i + 1}: Offline\n";
                        }


                        var Message = await Client.GetGuild(682882628168450079).GetTextChannel(696987038531977227).GetMessageAsync(697046877031366717) as IUserMessage;

                        bool Case2 = true;
                        try { Case2 = Message.Embeds.First().Description != MessageContent; }
                        catch { }

                        if (Message.Embeds.Count == 0 || Case2)
                        {
                            EmbedBuilder Embed = new EmbedBuilder();
                            if (Online == await GetTotalShards()) Embed = GetLargeEmbed("Shards", MessageContent).ToEmbedBuilder();
                            else Embed = GetLargeEmbed("Shards", MessageContent).ToEmbedBuilder().WithColor(181, 67, 67);

                            await Message.ModifyAsync(x => { x.Embed = Embed.Build(); x.Content = ""; });
                        }
                    }
                    catch { }

                    await Task.Delay(30000);
                }
            }
        }

        public static async Task FlushDisconnected(bool loop = true)
        {
            while (loop)
            {
                try
                {
                    var ShardData = GetShardData(-1, "Online");
                    ShardData.AddRange(GetShardData(-1, "Reserved"));

                    ShardData.RemoveAll(x => DateTime.Now - x.Heartbeat < TimeSpan.FromSeconds(20));

                    foreach (var OldData in ShardData)
                    {
                        DeleteData(OldData.ID);
                    }
                }
                catch { };

                await Task.Delay(1000);
            }

            if (!loop)
            {
                try
                {
                    var ShardData = GetShardData(-1, "Online");
                    ShardData.AddRange(GetShardData(-1, "Reserved"));

                    ShardData.RemoveAll(x => DateTime.Now - x.Heartbeat < TimeSpan.FromSeconds(30));

                    foreach (var OldData in ShardData)
                    {
                        DeleteData(OldData.ID);
                    }
                }
                catch { };
            }
        }

        public static List<ShardData> GetShardData(int ShardID = -1, string Type = null, DateTime? Heartbeat = null)
        {
            List<ShardData> Data = new List<ShardData>();

            using (var connection = new MySqlConnection(ConnectionString))
            {
                using (var command = connection.CreateCommand())
                {
                    connection.Open();

                    if (ShardID == -1 & Type == null & Heartbeat == null) throw new Exception();
                    string Command = "SELECT * FROM Utili_Shards WHERE(";
                    if (ShardID != -1)
                    {
                        Command += $"ShardID = @ShardID AND ";
                        command.Parameters.Add(new MySqlParameter("ShardID", ShardID));
                    }
                    if (Type != null)
                    {
                        Command += $"Type = @Type AND ";
                        command.Parameters.Add(new MySqlParameter("Type", Type));
                    }
                    if (Heartbeat.HasValue)
                    {
                        Command += $"DataValue = @Value";
                        command.Parameters.Add(new MySqlParameter("Heartbeat", ToSQLTime(Heartbeat.Value)));
                    }
                    if (Command.Substring(Command.Length - 5) == " AND ") Command = Command.Substring(0, Command.Length - 5);
                    Command += ");";


                    command.CommandText = Command;
                    MySqlDataReader DataReader = null;
                    try
                    {
                        DataReader = command.ExecuteReader();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                    while (DataReader.Read())
                    {
                        ShardData New;
                        try { New = new ShardData(DataReader.GetInt32(0), DataReader.GetInt32(1), DataReader.GetString(2), DataReader.GetDateTime(3)); }
                        catch { New = new ShardData(DataReader.GetInt32(0), DataReader.GetInt32(1), DataReader.GetString(2), DateTime.MaxValue); }

                        Data.Add(New);
                    }
                }
            }

            return Data;
        }

        public static void SaveData(int ShardID, string Type, DateTime? Heartbeat = null)
        {
            if(Heartbeat.HasValue) RunNonQuery($"INSERT INTO Utili_Shards(ShardID, Type, Heartbeat) VALUES(@ShardID, @Type, @Heartbeat);", new (string, string)[] { ("ShardID", ShardID.ToString()), ("Type", Type), ("Heartbeat", ToSQLTime(Heartbeat.Value)) });
            else RunNonQuery($"INSERT INTO Utili_Shards(ShardID, Type) VALUES(@ShardID, @Type);", new (string, string)[] { ("ShardID", ShardID.ToString()), ("Type", Type) });
        }

        public static void DeleteData(int ShardID = -1, string Type = null, DateTime? Heartbeat = null)
        {
            if (ShardID == -1 & Type == null & !Heartbeat.HasValue) throw new Exception();
            string Command = "DELETE FROM Utili_Shards WHERE(";
            if (ShardID != -1)
            {
                Command += $"ShardID = @ShardID AND ";
            }
            if (Type != null)
            {
                Command += $"Type = @Type AND ";
            }
            if (Heartbeat.HasValue)
            {
                Command += $"Heatbeat = @Heartbeat";
            }
            if (Command.Substring(Command.Length - 5) == " AND ") Command = Command.Substring(0, Command.Length - 5);
            Command += ");";

            if(Heartbeat.HasValue) RunNonQuery(Command, new (string, string)[] { ("ShardID", ShardID.ToString()), ("Type", Type), ("Heartbeat", ToSQLTime(Heartbeat.Value)) });
            else RunNonQuery(Command, new (string, string)[] { ("ShardID", ShardID.ToString()), ("Type", Type) });
        }

        public static void DeleteData(int ID)
        {
            RunNonQuery($"DELETE FROM Utili_Shards WHERE ID = {ID}");
        }
    }



    class ShardData
    {
        public int ID { get; }
        public int ShardID { get; }
        public string Type { get; }
        public DateTime Heartbeat { get; }

        public ShardData(int id, int shardid, string type, DateTime heartbeat)
        {
            ID = id;
            ShardID = shardid;
            Type = type;
            Heartbeat = heartbeat;
        }
    }
}
