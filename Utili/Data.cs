﻿using Discord.Commands;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using static Utili.Json;

namespace Utili
{
    internal class Data
    {
        public static List<Data> Cache;

        public static string ConnectionString = "";

        public static int Queries = 0;
        public static int CacheQueries = 0;
        public static DateTime QueryTimer = DateTime.Now;

        public static int ReactionsAltered = 0;
        public static double ReactionsAlteredPerSecond = 0;

        public static double QueriesPerSecond = 0;
        public static double CacheQueriesPerSecond = 0;
        public static int DBLatency = 0;
        public static int CacheItems = 0;
        public static int SendLatency = 0;
        public static int EditLatency = 0;

        public static List<Data> CommonItemsRegistry = new List<Data>();
        public static string CommonItemsOutput = "No data collected yet.";

        public static List<Data> CommonItemsGot = new List<Data>();
        public static string CommonItemsGotOutput = "No data collected yet.";

        public static List<Data> CommonItemsSaved = new List<Data>();
        public static string CommonItemsSavedOutput = "No data collected yet.";

        public static void SetConnectionString()
        {
            ConnectionString = $"Server={Config.Database.Server};Database={Config.Database.Database};Uid={Config.Database.Username};Pwd={Config.Database.Password};";
        }

        public static int RunNonQuery(string Command, (string, string)[] Values = null)
        {
            try
            {
                using (var connection = new MySqlConnection(ConnectionString))
                {
                    using (var command = connection.CreateCommand())
                    {
                        connection.Open();
                        command.CommandText = Command;

                        if (Values != null)
                        {
                            foreach (var Value in Values)
                            {
                                command.Parameters.Add(new MySqlParameter(Value.Item1, Value.Item2));
                            }
                        }

                        Queries += 1;
                        return command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public static void SaveData(string GuildID, string Type, string Value = "", bool IgnoreCache = false, bool CacheOnly = false, string Table = "Utili")
        {
            Data Data = new Data(GuildID, Type, Value);
            CommonItemsSaved.Add(Data);

            if (!IgnoreCache)
            {
                try { Cache.Add(new Data(GuildID, Type, Value)); } catch { };
                CacheQueries += 1;
            }

            if (!CacheOnly) RunNonQuery($"INSERT INTO {Table}(GuildID, DataType, DataValue) VALUES(@GuildID, @Type, @Value);", new (string, string)[] { ("GuildID", GuildID), ("Type", Type), ("Value", Value) });
        }

        public static List<Data> GetData(string GuildID = null, string Type = null, string Value = null, bool IgnoreCache = false, string Table = "Utili")
        {
            Data Data = new Data(GuildID, Type, Value);
            CommonItemsRegistry.Add(Data);

            return GetDataList(GuildID, Type, Value, IgnoreCache, Table);
        }

        public static Data GetFirstData(string GuildID = null, string Type = null, string Value = null, bool IgnoreCache = false, string Table = "Utili")
        {
            try
            {
                try { if (!Program.Client.Guilds.Select(x => x.Id).Contains(ulong.Parse(GuildID))) IgnoreCache = true; }
                catch { }

                if (!IgnoreCache)
                {
                    Data Data = new Data(GuildID, Type, Value);
                    CommonItemsRegistry.Add(Data);

                    CacheQueries += 1;
                    if (GuildID != null && Type != null && Value != null) return Cache.First(x => x.GuildID == GuildID && x.Type == Type && x.Value == Value);
                    else if (Type != null && Value != null) return Cache.First(x => x.Type == Type && x.Value == Value);
                    else if (GuildID != null && Value != null) return Cache.First(x => x.GuildID == GuildID && x.Value == Value);
                    else if (GuildID != null && Type != null) return Cache.First(x => x.GuildID == GuildID && x.Type == Type);
                    else if (Value != null) return Cache.First(x => x.Value == Value);
                    else if (GuildID != null) return Cache.First(x => x.GuildID == GuildID);
                    else if (Type != null) return Cache.First(x => x.Type == Type);
                    else return Cache.First();
                }

                return GetDataList(GuildID, Type, Value, true, Table).First();
            }
            catch
            {
                return null;
            }
        }

        public static bool DataExists(string GuildID = null, string Type = null, string Value = null, bool IgnoreCache = false, string Table = "Utili")
        {
            if (GetFirstData(GuildID, Type, Value, IgnoreCache, Table) != null) return true;
            else return false;
        }

        public static List<Data> GetDataList(string GuildID = null, string Type = null, string Value = null, bool IgnoreCache = false, string Table = "Utili")
        {
            List<Data> Data = new List<Data>();

            try { if (!Program.Client.Guilds.Select(x => x.Id).Contains(ulong.Parse(GuildID))) IgnoreCache = true; }
            catch { }

            if (!IgnoreCache)
            {
                CacheQueries += 1;
                if (GuildID != null && Type != null && Value != null) return Cache.Where(x => x.GuildID == GuildID && x.Type == Type && x.Value == Value).ToList();
                else if (Type != null && Value != null) return Cache.Where(x => x.Type == Type && x.Value == Value).ToList();
                else if (GuildID != null && Value != null) return Cache.Where(x => x.GuildID == GuildID && x.Value == Value).ToList();
                else if (GuildID != null && Type != null) return Cache.Where(x => x.GuildID == GuildID && x.Type == Type).ToList();
                else if (Value != null) return Cache.Where(x => x.Value == Value).ToList();
                else if (GuildID != null) return Cache.Where(x => x.GuildID == GuildID).ToList();
                else if (Type != null) return Cache.Where(x => x.Type == Type).ToList();
                else return Cache;
            }

            using (var connection = new MySqlConnection(ConnectionString))
            {
                using (var command = connection.CreateCommand())
                {
                    connection.Open();

                    string Command;
                    if (GuildID == null & Type == null & Value == null) Command = $"SELECT * FROM {Table};";
                    else
                    {
                        Command = $"SELECT * FROM {Table} WHERE(";
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
                        Data TData = new Data(GuildID, Type, Value);
                        CommonItemsGot.Add(TData);

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

        public static List<Data> GetDataWhere(string Where)
        {
            List<Data> Data = new List<Data>();

            Data TData = new Data("WHERE", "WHERE", Where);
            CommonItemsGot.Add(TData);

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

        public static void DeleteDataWhere(string Where, string Table)
        {
            RunNonQuery($"DELETE FROM {Table} WHERE {Where};");
        }

        public static void DeleteData(string GuildID = null, string Type = null, string Value = null, bool IgnoreCache = false, bool CacheOnly = false, string Table = "Utili")
        {
            if (GuildID == null & Type == null & Value == null) throw new Exception();

            if (!IgnoreCache)
            {
                List<Data> ToDelete = GetDataList(GuildID, Type, Value);
                foreach (Data Item in ToDelete) { Cache.Remove(Item); CacheQueries += 1; }
            }

            if (!CacheOnly)
            {
                string Command = $"DELETE FROM {Table} WHERE(";
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

        public static void SendEmail(string To, string Subject, string Content)
        {
            var fromAddress = new MailAddress(Config.EmailInfo.Username, "Utili Notifications");
            var toAddress = new MailAddress(To, To);
            string fromPassword = Config.EmailInfo.Password;
            string subject = Subject;
            string body = Content;

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                smtp.Send(message);
            }
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

        public override string ToString()
        {
            return $"{GuildID}, {Type}, {Value}";
        }
    }

    internal class MessageData
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