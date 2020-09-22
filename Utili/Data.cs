using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Discord.Commands;
using MySql.Data.MySqlClient;
using static Utili.Json;

namespace Utili
{
    internal class Data
    {
        public static List<Data> Cache;

        public static string ConnectionString = "";

        public static int Queries;
        public static int CacheQueries;
        public static DateTime QueryTimer = DateTime.Now;

        public static int ReactionsAltered = 0;
        public static double ReactionsAlteredPerSecond = 0;
        public static double MaxReactionsAlteredPerSecond = 0;

        public static double QueriesPerSecond = 0;
        public static double CacheQueriesPerSecond = 0;
        public static int DbLatency = 0;
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

        public static int RunNonQuery(string commandText, (string, string)[] values = null)
        {
            try
            {
                using MySqlConnection connection = new MySqlConnection(ConnectionString);
                using MySqlCommand command = connection.CreateCommand();
                connection.Open();
                command.CommandText = commandText;

                if (values != null)
                {
                    foreach ((string, string) value in values)
                    {
                        command.Parameters.Add(new MySqlParameter(value.Item1, value.Item2));
                    }
                }

                Queries += 1;
                return command.ExecuteNonQuery();
            }
            catch
            {
                return 0;
            }
        }

        public static void SaveData(string guildId, string type, string value = "", bool ignoreCache = false, bool cacheOnly = false, string table = "Utili")
        {
            Data data = new Data(guildId, type, value);
            CommonItemsSaved.Add(data);

            if (!ignoreCache)
            {
                try { Cache.Add(new Data(guildId, type, value)); } catch { }
                CacheQueries += 1;
            }

            if (!cacheOnly) RunNonQuery($"INSERT INTO {table}(GuildID, DataType, DataValue) VALUES(@GuildID, @Type, @Value);", new[] { ("GuildID", guildId), ("Type", type), ("Value", value) });
        }

        public static List<Data> GetData(string guildId = null, string type = null, string value = null, bool ignoreCache = false, string table = "Utili")
        {
            Data data = new Data(guildId, type, value);
            CommonItemsRegistry.Add(data);

            return GetDataList(guildId, type, value, ignoreCache, table);
        }

        public static Data GetFirstData(string guildId = null, string type = null, string value = null, bool ignoreCache = false, string table = "Utili")
        {
            try
            {
                try { if (!Program.Client.Guilds.Select(x => x.Id).Contains(ulong.Parse(guildId))) ignoreCache = true; }
                catch { }

                if (!ignoreCache)
                {
                    Data data = new Data(guildId, type, value);
                    CommonItemsRegistry.Add(data);

                    CacheQueries += 1;
                    if (guildId != null && type != null && value != null) return Cache.First(x => x.GuildId == guildId && x.Type == type && x.Value == value);
                    if (type != null && value != null) return Cache.First(x => x.Type == type && x.Value == value);
                    if (guildId != null && value != null) return Cache.First(x => x.GuildId == guildId && x.Value == value);
                    if (guildId != null && type != null) return Cache.First(x => x.GuildId == guildId && x.Type == type);
                    if (value != null) return Cache.First(x => x.Value == value);
                    if (guildId != null) return Cache.First(x => x.GuildId == guildId);
                    if (type != null) return Cache.First(x => x.Type == type);
                    return Cache.First();
                }

                return GetDataList(guildId, type, value, true, table).First();
            }
            catch
            {
                return null;
            }
        }

        public static bool DataExists(string guildId = null, string type = null, string value = null, bool ignoreCache = false, string table = "Utili")
        {
            if (GetFirstData(guildId, type, value, ignoreCache, table) != null) return true;
            return false;
        }

        public static List<Data> GetDataList(string guildId = null, string type = null, string value = null, bool ignoreCache = false, string table = "Utili")
        {
            List<Data> data = new List<Data>();

            try { if (!Program.Client.Guilds.Select(x => x.Id).Contains(ulong.Parse(guildId))) ignoreCache = true; }
            catch { }

            if (!ignoreCache)
            {
                CacheQueries += 1;
                if (guildId != null && type != null && value != null) return Cache.Where(x => x.GuildId == guildId && x.Type == type && x.Value == value).ToList();
                if (type != null && value != null) return Cache.Where(x => x.Type == type && x.Value == value).ToList();
                if (guildId != null && value != null) return Cache.Where(x => x.GuildId == guildId && x.Value == value).ToList();
                if (guildId != null && type != null) return Cache.Where(x => x.GuildId == guildId && x.Type == type).ToList();
                if (value != null) return Cache.Where(x => x.Value == value).ToList();
                if (guildId != null) return Cache.Where(x => x.GuildId == guildId).ToList();
                if (type != null) return Cache.Where(x => x.Type == type).ToList();
                return Cache;
            }

            using MySqlConnection connection = new MySqlConnection(ConnectionString);
            using MySqlCommand command = connection.CreateCommand();
            connection.Open();

            string commandText;
            if (guildId == null & type == null & value == null) commandText = $"SELECT * FROM {table};";
            else
            {
                commandText = $"SELECT * FROM {table} WHERE(";
                if (guildId != null)
                {
                    commandText += "GuildID = @GuildID AND ";
                    command.Parameters.Add(new MySqlParameter("GuildID", guildId));
                }
                if (type != null)
                {
                    commandText += "DataType = @Type AND ";
                    command.Parameters.Add(new MySqlParameter("Type", type));
                }
                if (value != null)
                {
                    commandText += "DataValue = @Value";
                    command.Parameters.Add(new MySqlParameter("Value", value));
                }
                if (commandText.Substring(commandText.Length - 5) == " AND ") commandText = commandText.Substring(0, commandText.Length - 5);
                commandText += ");";
            }

            command.CommandText = commandText;
            MySqlDataReader dataReader = null;
            try
            {
                CommonItemsGot.Add(new Data(guildId, type, value));

                Queries += 1;
                dataReader = command.ExecuteReader();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            while (dataReader.Read())
            {
                Data @new = new Data(dataReader.GetString(1), dataReader.GetString(2), dataReader.GetString(3))
                {
                    Id = dataReader.GetInt32(0)
                };
                data.Add(@new);
            }

            return data;
        }

        public static List<Data> GetDataWhere(string where)
        {
            List<Data> data = new List<Data>();

            CommonItemsGot.Add(new Data("WHERE", "WHERE", where));

            using MySqlConnection connection = new MySqlConnection(ConnectionString);
            using MySqlCommand command = connection.CreateCommand();
            connection.Open();

            string commandText = $"SELECT * FROM Utili WHERE({@where});";

            command.CommandText = commandText;
            MySqlDataReader dataReader = null;
            try
            {
                Queries += 1;
                dataReader = command.ExecuteReader();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            while (dataReader.Read())
            {
                Data @new = new Data(dataReader.GetString(1), dataReader.GetString(2), dataReader.GetString(3))
                {
                    Id = dataReader.GetInt32(0)
                };
                data.Add(@new);
            }

            return data;
        }

        public static void DeleteDataWhere(string where, string table)
        {
            RunNonQuery($"DELETE FROM {table} WHERE {where};");
        }

        public static void DeleteData(string guildId = null, string type = null, string value = null, bool ignoreCache = false, bool cacheOnly = false, string table = "Utili")
        {
            if (guildId == null & type == null & value == null) throw new Exception();

            if (!ignoreCache)
            {
                List<Data> toDelete = GetDataList(guildId, type, value);
                foreach (Data item in toDelete) { Cache.Remove(item); CacheQueries += 1; }
            }

            if (!cacheOnly)
            {
                string command = $"DELETE FROM {table} WHERE(";
                if (guildId != null)
                {
                    command += "GuildID = @GuildID AND ";
                }
                if (type != null)
                {
                    command += "DataType = @Type AND ";
                }
                if (value != null)
                {
                    command += "DataValue = @Value";
                }
                if (command.Substring(command.Length - 5) == " AND ") command = command.Substring(0, command.Length - 5);
                command += ");";

                RunNonQuery(command, new[] { ("GuildID", guildId), ("Type", type), ("Value", value) });
            }
        }

        public static async Task SaveMessageAsync(SocketCommandContext context)
        {
            string content = MessageLogs.Encrypt(context.Message.Content, context.Guild.Id, context.Channel.Id);

            RunNonQuery("INSERT INTO Utili_MessageLogs(GuildID, ChannelID, MessageID, UserID, Content, Timestamp) VALUES(@GuildID, @ChannelID, @MessageID, @UserID, @Content, @Timestamp);", new[] {
                ("GuildID", context.Guild.Id.ToString()),
                ("ChannelID", context.Channel.Id.ToString()),
                ("MessageID", context.Message.Id.ToString()),
                ("UserID", context.User.Id.ToString()),
                ("Content", content),
                ("Timestamp", ToSqlTime(context.Message.CreatedAt.DateTime)) });
        }

        public static async Task<MessageData> GetMessageAsync(ulong messageId)
        {
            using MySqlConnection connection = new MySqlConnection(ConnectionString);
            using MySqlCommand command = connection.CreateCommand();
            connection.Open();

            string commandText = $"SELECT * FROM Utili_MessageLogs WHERE MessageID = '{messageId}'";

            command.CommandText = commandText;
            MySqlDataReader dataReader = null;
            try
            {
                Queries += 1;
                dataReader = command.ExecuteReader();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            while (dataReader.Read())
            {
                MessageData data = new MessageData
                {
                    Id = dataReader.GetInt32(0),
                    GuildId = dataReader.GetString(1),
                    ChannelId = dataReader.GetString(2),
                    MessageId = dataReader.GetString(3),
                    UserId = dataReader.GetString(4),
                    EncryptedContent = dataReader.GetString(5),
                    Timestmap = dataReader.GetDateTime(6)
                };

                return data;
            }

            return null;
        }

        public static string ToSqlTime(DateTime time)
        {
            return $"{time.Year:0000}-{time.Month:00}-{time.Day:00} {time.Hour:00}:{time.Minute:00}:{time.Second:00}";
        }

        public static void SendEmail(string to, string subject, string body)
        {
            MailAddress fromAddress = new MailAddress(Config.EmailInfo.Username, "Utili Notifications");
            MailAddress toAddress = new MailAddress(to, to);
            string fromPassword = Config.EmailInfo.Password;

            SmtpClient smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };
            using MailMessage message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            };
            smtp.Send(message);
        }

        public int Id;
        public string GuildId;
        public string Type;
        public string Value;

        public Data(string guildid, string type, string value)
        {
            GuildId = guildid;
            Type = type;
            Value = value;
        }

        public override string ToString()
        {
            return $"{GuildId}, {Type}, {Value}";
        }
    }

    internal class MessageData
    {
        public int Id;
        public string GuildId;
        public string ChannelId;
        public string MessageId;
        public string UserId;
        public string EncryptedContent;
        public DateTime Timestmap;
    }
}