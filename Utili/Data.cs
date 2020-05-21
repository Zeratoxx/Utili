using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using Discord.Commands;

using static Utili.Json;
using System.Linq;

namespace Utili
{
    class Data
    {
        public static HashSet<Data> Cache;

        public static string ConnectionString = "";

        public static int Queries = 0;
        public static int CacheQueries = 0;
        public static DateTime QueryTimer = DateTime.Now;

        public static double QueriesPerSecond = 0;
        public static double CacheQueriesPerSecond = 0;
        public static int DBLatency = 0;
        public static int DatabaseItems = 0;
        public static int CacheLatency = 0;
        public static int CacheItems = 0;
        public static int SendLatency = 0;
        public static int EditLatency = 0;

        public static void SetConnectionString()
        {
            ConnectionString = $"Server={Config.Database.Server};Database={Config.Database.Database};Uid={Config.Database.Username};Pwd={Config.Database.Password};";
        }

        public static string RunNonQuery(string Command, (string, string)[] Values = null)
        {
            try
            {
                using (var connection = new MySqlConnection(ConnectionString))
                {
                    using (var command = connection.CreateCommand())
                    {
                        connection.Open();
                        command.CommandText = Command;

                        if(Values != null)
                        {
                            foreach (var Value in Values)
                            {
                                command.Parameters.Add(new MySqlParameter(Value.Item1, Value.Item2));
                            }
                        }

                        Queries += 1;
                        command.ExecuteNonQuery();
                    }
                }
                return "Success";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public static void SaveData(string GuildID, string Type, string Value = "", bool IgnoreCache = false, bool CacheOnly = false)
        {
            if (!IgnoreCache)
            {
                try { Cache.Add(new Data(GuildID, Type, Value)); } catch { };
                CacheQueries += 1;
            }

            if(!CacheOnly) RunNonQuery($"INSERT INTO Utili(GuildID, DataType, DataValue) VALUES(@GuildID, @Type, @Value);", new (string, string)[] { ("GuildID", GuildID), ("Type", Type), ("Value", Value) });
        }

        public static List<Data> GetData(string GuildID = null, string Type = null, string Value = null, bool IgnoreCache = false)
        {
            HashSet<Data> Data = new HashSet<Data>();

            if (!IgnoreCache)
            {
                Data = Cache;
                if (GuildID != null) Data = Data.Where(x => x.GuildID == GuildID).ToHashSet();
                if (Type != null) Data = Data.Where(x => x.Type == Type).ToHashSet();
                if (Value != null) Data = Data.Where(x => x.Value == Value).ToHashSet();
                CacheQueries += 1;
                return Data.ToList();
            }

            using (var connection = new MySqlConnection(ConnectionString))
            {
                using (var command = connection.CreateCommand())
                {
                    connection.Open();

                    string Command;
                    if (GuildID == null & Type == null & Value == null) Command = "SELECT * FROM Utili;";
                    else
                    {
                        Command = "SELECT * FROM Utili WHERE(";
                        if (GuildID != null)
                        {
                            Command += $"GuildID = @GuildID AND ";
                            command.Parameters.Add(new MySqlParameter("GuildID", GuildID));
                        }
                        if (Type != null)
                        {
                            Command += $"DataType = @Type AND ";
                            command.Parameters.Add(new MySqlParameter("Type", Type));
                        }
                        if (Value != null)
                        {
                            Command += $"DataValue = @Value";
                            command.Parameters.Add(new MySqlParameter("Value", Value));
                        }
                        if (Command.Substring(Command.Length - 5) == " AND ") Command = Command.Substring(0, Command.Length - 5);
                        Command += ");";
                    }

                    command.CommandText = Command;
                    MySqlDataReader DataReader = null;
                    try
                    {
                        Queries += 1;
                        DataReader = command.ExecuteReader();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                    while (DataReader.Read())
                    {
                        Data New = new Data(DataReader.GetString(1), DataReader.GetString(2), DataReader.GetString(3));
                        New.ID = DataReader.GetInt32(0);
                        Data.Add(New);
                    }
                }
            }

            return Data.ToList();
        }

        public static List<Data> GetDataWhere(string Where)
        {
            List<Data> Data = new List<Data>();

            using (var connection = new MySqlConnection(ConnectionString))
            {
                using (var command = connection.CreateCommand())
                {
                    connection.Open();

                    string Command = $"SELECT * FROM Utili WHERE({Where});";

                    command.CommandText = Command;
                    MySqlDataReader DataReader = null;
                    try
                    {
                        Queries += 1;
                        DataReader = command.ExecuteReader();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                    while (DataReader.Read())
                    {
                        Data New = new Data(DataReader.GetString(1), DataReader.GetString(2), DataReader.GetString(3));
                        New.ID = DataReader.GetInt32(0);
                        Data.Add(New);
                    }
                }
            }

            return Data;
        }

        public static void DeleteData(string GuildID = null, string Type = null, string Value = null, bool IgnoreCache = false, bool CacheOnly = false)
        {
            if (GuildID == null & Type == null & Value == null) throw new Exception();

            if (!IgnoreCache)
            {
                HashSet<Data> ToDelete = Cache;
                if (GuildID != null) ToDelete = ToDelete.Where(x => x.GuildID == GuildID).ToHashSet();
                if (Type != null) ToDelete = ToDelete.Where(x => x.Type == Type).ToHashSet();
                if (Value != null) ToDelete = ToDelete.Where(x => x.Value == Value).ToHashSet();
                foreach (Data Item in ToDelete) { Cache.Remove(Item); CacheQueries += 1; }
            }

            if (!CacheOnly)
            {
                string Command = "DELETE FROM Utili WHERE(";
                if (GuildID != null)
                {
                    Command += $"GuildID = @GuildID AND ";
                }
                if (Type != null)
                {
                    Command += $"DataType = @Type AND ";
                }
                if (Value != null)
                {
                    Command += $"DataValue = @Value";
                }
                if (Command.Substring(Command.Length - 5) == " AND ") Command = Command.Substring(0, Command.Length - 5);
                Command += ");";

                RunNonQuery(Command, new (string, string)[] { ("GuildID", GuildID), ("Type", Type), ("Value", Value) });
            }
        }

        public static async Task SaveMessageAsync(SocketCommandContext Context)
        {
            string Content = MessageLogs.Encrypt(Context.Message.Content, Context.Guild.Id, Context.Channel.Id);

            RunNonQuery($"INSERT INTO Utili_MessageLogs(GuildID, ChannelID, MessageID, UserID, Content, Timestamp) VALUES(@GuildID, @ChannelID, @MessageID, @UserID, @Content, @Timestamp);", new (string, string)[] {
                ("GuildID", Context.Guild.Id.ToString()),
                ("ChannelID", Context.Channel.Id.ToString()),
                ("MessageID", Context.Message.Id.ToString()),
                ("UserID", Context.User.Id.ToString()),
                ("Content", Content),
                ("Timestamp", ToSQLTime(Context.Message.CreatedAt.DateTime)) });
        }

        public static async Task<MessageData> GetMessageAsync(ulong MessageID)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                using (var command = connection.CreateCommand())
                {
                    connection.Open();

                    string Command = $"SELECT * FROM Utili_MessageLogs WHERE MessageID = '{MessageID}'";

                    command.CommandText = Command;
                    MySqlDataReader DataReader = null;
                    try
                    {
                        Queries += 1;
                        DataReader = command.ExecuteReader();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                    while (DataReader.Read())
                    {
                        MessageData Data = new MessageData();
                        Data.ID = DataReader.GetInt32(0);
                        Data.GuildID = DataReader.GetString(1);
                        Data.ChannelID = DataReader.GetString(2);
                        Data.MessageID = DataReader.GetString(3);
                        Data.UserID = DataReader.GetString(4);
                        Data.EncryptedContent = DataReader.GetString(5);
                        Data.Timestmap = DataReader.GetDateTime(6);

                        return Data;
                    }

                    return null;
                }
            }
        }

        public static string ToSQLTime(DateTime Time)
        {
            return $"{Time.Year.ToString("0000")}-{Time.Month.ToString("00")}-{Time.Day.ToString("00")} {Time.Hour.ToString("00")}:{Time.Minute.ToString("00")}:{Time.Second.ToString("00")}";
        }

        public int ID = 0;
        public string GuildID;
        public string Type;
        public string Value;

        public Data(string guildid, string type, string value)
        {
            GuildID = guildid;
            Type = type;
            Value = value;
        }
    }

    class MessageData
    {
        public int ID;
        public string GuildID;
        public string ChannelID;
        public string MessageID;
        public string UserID;
        public string EncryptedContent;
        public DateTime Timestmap;
    }
}
