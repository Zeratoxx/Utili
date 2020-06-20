﻿using System;
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
using System.Net.Mail;

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
            while (!Program.ForceStop.IsCancellationRequested)
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

                await Task.Delay(1000);
            }
        }

        public static List<int> OfflineShardIDs = new List<int>();

        public static async Task FlushDisconnected(bool loop = true)
        {
            if (loop)
            {
                while (!Program.ForceStop.IsCancellationRequested)
                {
                    try
                    {
                        bool AllowOnlineNotification = true;

                        var ShardData = GetShardData(-1, "Online");
                        ShardData.AddRange(GetShardData(-1, "Reserved"));

                        ShardData.RemoveAll(x => DateTime.Now - x.Heartbeat < TimeSpan.FromSeconds(20));

                        foreach (var OldData in ShardData)
                        {
                            DeleteData(OldData.ID);

                            if (!OfflineShardIDs.Contains(OldData.ShardID) && Program.Ready)
                            {
                                AllowOnlineNotification = false;
                                OfflineShardIDs.Add(OldData.ShardID);
                                await Program.Shards.GetUser(218613903653863427).SendMessageAsync(embed: GetEmbed("No", "Shard offline", $"Shard {OldData.ShardID} has stopped sending a heartbeat.\nLots of love, shard {Program.Client.ShardId}."));

                                SendEmail(Config.EmailInfo.Username, $"Shard offline", $"Shard {OldData.ShardID} has stopped sending a heartbeat.\nLots of love, shard {Program.Client.ShardId}");
                            }
                        }

                        if (AllowOnlineNotification && Program.Ready)
                        {
                            foreach (int OfflineShardID in OfflineShardIDs)
                            {
                                if (ShardData.Where(x => x.ShardID == OfflineShardID).Count() > 0)
                                {
                                    await Program.Shards.GetUser(218613903653863427).SendMessageAsync(embed: GetEmbed("Yes", "Shard online", $"Shard {OfflineShardID} has sent a heartbeat.\nLots of love, shard {Program.Client.ShardId}."));
                                    OfflineShardIDs.Remove(OfflineShardID);
                                }
                            }
                        }
                    }
                    catch { };

                    await Task.Delay(1000);
                }
            }
            else
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
