using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using static Utili.Data;
using static Utili.Json;
using static Utili.SendMessage;

namespace Utili
{
    internal class Sharding
    {
        public static bool GettingShard;

        public static async Task<int> GetShardId()
        {
            GettingShard = true;

            while (true)
            {
                try
                {
                    Random random = new Random();

                    int shards = await GetTotalShards();

                    while (true)
                    {
                        int target = random.Next(0, shards);

                        if (GetShardData(target, "Online").Count == 0 && GetShardData(target, "Reserved").Count == 0)
                        {
                            SaveData(target, "Reserved", DateTime.Now);
                            await Task.Delay(2500);

                            if (GetShardData(target, "Reserved").Count == 1 && GetShardData(target, "Online").Count == 0)
                            {
                                SaveData(target, "Online", DateTime.Now);
                                DeleteData(target, "Reserved");

                                GettingShard = false;

                                return target;
                            }

                            DeleteData(target, "Reserved");
                        }

                        await FlushDisconnected(false);
                    }
                }
                catch { }
            }
        }

        public static async Task<int> GetTotalShards()
        {
            return GetShardData(type: "ShardCount").First().ShardId;
        }

        public static async Task KeepConnection()
        {
            while (!Program.ForceStop.IsCancellationRequested && !GettingShard)
            {
                try
                {
                    SaveData(Program.Client.ShardId, "Online", DateTime.Now);

                    List<ShardData> shardData = GetShardData(Program.Client.ShardId, "Online");
                    shardData = shardData.OrderBy(x => x.Heartbeat).ToList();
                    shardData.Reverse();
                    shardData.RemoveAt(0);

                    await Task.Delay(500);

                    foreach (ShardData oldData in shardData)
                    {
                        DeleteData(oldData.Id);
                    }

                    await Task.Delay(1000);
                }
                catch { await Task.Delay(1000); }
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
                        bool allowOnlineNotification = true;

                        List<ShardData> shardData = GetShardData(-1, "Online");
                        shardData.AddRange(GetShardData(-1, "Reserved"));

                        List<ShardData> oldShardData = shardData.Where(x => DateTime.Now - x.Heartbeat > TimeSpan.FromSeconds(10)).ToList();
                        oldShardData.AddRange(shardData.Where(x => DateTime.Now - x.Heartbeat < TimeSpan.FromMinutes(-1)));
                        // ShardData is now all shards which were active but are now not sending a heartbeat.

                        foreach (ShardData oldData in shardData.Where(x => x.ShardId != Program.ShardId))
                        {
                            allowOnlineNotification = false;
                            DeleteData(oldData.Id);

                            if (!OfflineShardIDs.Contains(oldData.ShardId))
                            {
                                OfflineShardIDs.Add(oldData.ShardId);
                                SendEmail(Config.EmailInfo.Username, "Shard offline", $"Shard {oldData.ShardId} has stopped sending a heartbeat.\nSent by shard {Program.Client.ShardId}");
                                await Program.Shards.GetGuild(682882628168450079).GetTextChannel(731790673728241665).SendMessageAsync("<@!218613903653863427>", embed: GetEmbed("No", "Shard offline", $"Shard {oldData.ShardId} has stopped sending a heartbeat.\nSent by shard {Program.Client.ShardId}."));
                            }
                        }

                        if (allowOnlineNotification && Program.Ready)
                        {
                            foreach (int offlineShardId in OfflineShardIDs)
                            {
                                if (GetShardData(offlineShardId, "Online").Count > 0)
                                {
                                    await Program.Shards.GetGuild(682882628168450079).GetTextChannel(731790673728241665).SendMessageAsync("<@!218613903653863427>", embed: GetEmbed("Yes", "Shard online", $"Shard {offlineShardId} has sent a heartbeat.\nSent by shard {Program.Client.ShardId}."));
                                    OfflineShardIDs.Remove(offlineShardId);
                                }
                            }
                        }
                    }
                    catch { }

                    await Task.Delay(3000);
                }
            }
            else
            {
                try
                {
                    List<ShardData> shardData = GetShardData(-1, "Online");
                    shardData.AddRange(GetShardData(-1, "Reserved"));

                    shardData.RemoveAll(x => DateTime.Now - x.Heartbeat < TimeSpan.FromSeconds(10));

                    foreach (ShardData oldData in shardData)
                    {
                        DeleteData(oldData.Id);
                    }
                }
                catch { }
            }
        }

        public static List<ShardData> GetShardData(int shardId = -1, string type = null, DateTime? heartbeat = null)
        {
            List<ShardData> data = new List<ShardData>();

            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                using (MySqlCommand command = connection.CreateCommand())
                {
                    connection.Open();

                    if (shardId == -1 & type == null & heartbeat == null) throw new Exception();
                    string commandText = "SELECT * FROM Utili_Shards WHERE(";
                    if (shardId != -1)
                    {
                        commandText += "ShardID = @ShardID AND ";
                        command.Parameters.Add(new MySqlParameter("ShardID", shardId));
                    }
                    if (type != null)
                    {
                        commandText += "Type = @Type AND ";
                        command.Parameters.Add(new MySqlParameter("Type", type));
                    }
                    if (heartbeat.HasValue)
                    {
                        commandText += "DataValue = @Value";
                        command.Parameters.Add(new MySqlParameter("Heartbeat", ToSqlTime(heartbeat.Value)));
                    }
                    if (commandText.Substring(commandText.Length - 5) == " AND ") commandText = commandText.Substring(0, commandText.Length - 5);
                    commandText += ");";

                    command.CommandText = commandText;
                    MySqlDataReader dataReader = null;
                    try
                    {
                        dataReader = command.ExecuteReader();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                    while (dataReader.Read())
                    {
                        ShardData @new;
                        try { @new = new ShardData(dataReader.GetInt32(0), dataReader.GetInt32(1), dataReader.GetString(2), dataReader.GetDateTime(3)); }
                        catch { @new = new ShardData(dataReader.GetInt32(0), dataReader.GetInt32(1), dataReader.GetString(2), DateTime.MaxValue); }

                        data.Add(@new);
                    }
                }
            }

            return data;
        }

        public static void SaveData(int shardId, string type, DateTime? heartbeat = null)
        {
            if (heartbeat.HasValue) RunNonQuery("INSERT INTO Utili_Shards(ShardID, Type, Heartbeat) VALUES(@ShardID, @Type, @Heartbeat);", new[] { ("ShardID", shardId.ToString()), ("Type", type), ("Heartbeat", ToSqlTime(heartbeat.Value)) });
            else RunNonQuery("INSERT INTO Utili_Shards(ShardID, Type) VALUES(@ShardID, @Type);", new[] { ("ShardID", shardId.ToString()), ("Type", type) });
        }

        public static void DeleteData(int shardId = -1, string type = null, DateTime? heartbeat = null)
        {
            if (shardId == -1 & type == null & !heartbeat.HasValue) throw new Exception();
            string command = "DELETE FROM Utili_Shards WHERE(";
            if (shardId != -1)
            {
                command += "ShardID = @ShardID AND ";
            }
            if (type != null)
            {
                command += "Type = @Type AND ";
            }
            if (heartbeat.HasValue)
            {
                command += "Heatbeat = @Heartbeat";
            }
            if (command.Substring(command.Length - 5) == " AND ") command = command.Substring(0, command.Length - 5);
            command += ");";

            if (heartbeat.HasValue) RunNonQuery(command, new[] { ("ShardID", shardId.ToString()), ("Type", type), ("Heartbeat", ToSqlTime(heartbeat.Value)) });
            else RunNonQuery(command, new[] { ("ShardID", shardId.ToString()), ("Type", type) });
        }

        public static void DeleteData(int id)
        {
            RunNonQuery($"DELETE FROM Utili_Shards WHERE ID = {id}");
        }
    }

    internal class ShardData
    {
        public int Id { get; }
        public int ShardId { get; }
        public string Type { get; }
        public DateTime Heartbeat { get; }

        public ShardData(int id, int shardid, string type, DateTime heartbeat)
        {
            Id = id;
            ShardId = shardid;
            Type = type;
            Heartbeat = heartbeat;
        }
    }
}