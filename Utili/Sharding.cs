using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Utili.Data;
using static Utili.Json;
using static Utili.SendMessage;

namespace Utili
{
    internal class Sharding
    {
        public static bool GettingShard = false;
        public static async Task<int> GetShardID()
        {
            GettingShard = true;

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

                                GettingShard = false;

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
            while (!Program.ForceStop.IsCancellationRequested && !GettingShard)
            {
                try
                {
                    SaveData(Program.Client.ShardId, "Online", DateTime.Now);

                    var ShardData = GetShardData(Program.Client.ShardId, "Online");
                    ShardData = ShardData.OrderBy(x => x.Heartbeat).ToList();
                    ShardData.Reverse();
                    ShardData.RemoveAt(0);

                    await Task.Delay(500);

                    foreach (var OldData in ShardData)
                    {
                        DeleteData(OldData.ID);
                    }

                    await Task.Delay(1000);
                }
                catch { await Task.Delay(1000); };
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

                        ShardData.RemoveAll(x => DateTime.Now - x.Heartbeat < TimeSpan.FromSeconds(10));
                        // ShardData is now all shards which were active but are now not sending a heartbeat.

                        foreach (var OldData in ShardData.Where(x => x.ShardID != Program.ShardID))
                        {
                            AllowOnlineNotification = false;
                            DeleteData(OldData.ID);

                            if (!OfflineShardIDs.Contains(OldData.ShardID))
                            {
                                OfflineShardIDs.Add(OldData.ShardID);
                                SendEmail(Config.EmailInfo.Username, $"Shard offline", $"Shard {OldData.ShardID} has stopped sending a heartbeat.\nSent by shard {Program.Client.ShardId}");
                                await Program.Shards.GetGuild(682882628168450079).GetTextChannel(731790673728241665).SendMessageAsync("<@!218613903653863427>", embed: GetEmbed("No", "Shard offline", $"Shard {OldData.ShardID} has stopped sending a heartbeat.\nSent by shard {Program.Client.ShardId}."));
                            }
                        }

                        if (AllowOnlineNotification && Program.Ready)
                        {
                            foreach (int OfflineShardID in OfflineShardIDs)
                            {
                                if (GetShardData(OfflineShardID, "Online").Count > 0)
                                {
                                    await Program.Shards.GetGuild(682882628168450079).GetTextChannel(731790673728241665).SendMessageAsync("<@!218613903653863427>", embed: GetEmbed("Yes", "Shard online", $"Shard {OfflineShardID} has sent a heartbeat.\nSent by shard {Program.Client.ShardId}."));
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

                    ShardData.RemoveAll(x => DateTime.Now - x.Heartbeat < TimeSpan.FromSeconds(10));

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
            if (Heartbeat.HasValue) RunNonQuery($"INSERT INTO Utili_Shards(ShardID, Type, Heartbeat) VALUES(@ShardID, @Type, @Heartbeat);", new (string, string)[] { ("ShardID", ShardID.ToString()), ("Type", Type), ("Heartbeat", ToSQLTime(Heartbeat.Value)) });
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

            if (Heartbeat.HasValue) RunNonQuery(Command, new (string, string)[] { ("ShardID", ShardID.ToString()), ("Type", Type), ("Heartbeat", ToSQLTime(Heartbeat.Value)) });
            else RunNonQuery(Command, new (string, string)[] { ("ShardID", ShardID.ToString()), ("Type", Type) });
        }

        public static void DeleteData(int ID)
        {
            RunNonQuery($"DELETE FROM Utili_Shards WHERE ID = {ID}");
        }
    }

    internal class ShardData
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