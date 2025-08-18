using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

namespace warframetgbot.Db.Notification
{
    public static class DataBaseFissure
    {
        public static async Task Initialize(string dbPath)
        {
            try
            {
                // Формируем полный путь
                string fullDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbPath);
                Console.WriteLine($"DatabaseInitializer: Attempting to initialize database at {fullDbPath}");

                // Проверяем и создаём директорию, если её нет
                string dbDirectory = Path.GetDirectoryName(fullDbPath);
                if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
                {
                    Directory.CreateDirectory(dbDirectory);
                    Console.WriteLine($"DatabaseInitializer: Created directory {dbDirectory}");
                }

                // Проверяем или создаём файл БД
                if (!File.Exists(fullDbPath))
                {
                    SQLiteConnection.CreateFile(fullDbPath);
                    Console.WriteLine($"DatabaseInitializer: Created database file {fullDbPath}");
                }

                // Подключаемся к БД
                using (var connection = new SQLiteConnection($"Data Source={fullDbPath}"))
                {
                    await connection.OpenAsync();

                    // Удаляем таблицу Fissures, если она существует
                    string dropTableQuery = "DROP TABLE IF EXISTS Fissures";
                    using (var command = new SQLiteCommand(dropTableQuery, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                        Console.WriteLine($"DatabaseInitializer: Dropped table Fissures if it existed");
                    }

                    // Удаляем таблицу Users, если она существует
                    string dropTableQueryUsers = "DROP TABLE IF EXISTS Users";
                    using (var command = new SQLiteCommand(dropTableQueryUsers, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                        Console.WriteLine($"DatabaseInitializer: Dropped table Users if it existed");
                    }

                    // Создаём таблицу Users
                    var createTableQueryUsers = new SQLiteCommand(@"
                        CREATE TABLE IF NOT EXISTS Users (
                            UserId INTEGER PRIMARY KEY,
                            IsHardEnabled TEXT NOT NULL,
                            PlanetEnabled TEXT NOT NULL,
                            TierEnabled TEXT NOT NULL,
                            MissionEnabled TEXT NOT NULL
                        )", connection);
                    await createTableQueryUsers.ExecuteNonQueryAsync();
                    Console.WriteLine($"DatabaseInitializer: Created table Users");

                    // Создаём таблицу Fissures
                    var createTableQuery = new SQLiteCommand(@"
                        CREATE TABLE IF NOT EXISTS Fissures (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Node TEXT NOT NULL,
                            MissionType TEXT NOT NULL,
                            Tier TEXT NOT NULL,
                            Enemy TEXT NOT NULL,
                            Eta TEXT NOT NULL,
                            IsHard INTEGER NOT NULL,
                            TierNum INTEGER NOT NULL,
                            Timestamp TEXT NOT NULL
                        )", connection);
                    await createTableQuery.ExecuteNonQueryAsync();
                    Console.WriteLine($"DatabaseInitializer: Created table Fissures");

                    // Выводим схему таблицы Users
                    Console.WriteLine("DatabaseInitializer: Schema for table Users:");
                    using (var command = new SQLiteCommand("PRAGMA table_info(Users);", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            Console.WriteLine("cid | name             | type    | notnull | dflt_value | pk");
                            Console.WriteLine("----+------------------+---------+---------+------------+---");
                            while (await reader.ReadAsync())
                            {
                                Console.WriteLine($"{reader["cid"],-3} | {reader["name"],-16} | {reader["type"],-7} | {reader["notnull"],-7} | {reader["dflt_value"] ?? "NULL",-10} | {reader["pk"]}");
                            }
                        }
                    }

                    // Выводим схему таблицы Fissures
                    Console.WriteLine("\nDatabaseInitializer: Schema for table Fissures:");
                    using (var command = new SQLiteCommand("PRAGMA table_info(Fissures);", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            Console.WriteLine("cid | name             | type    | notnull | dflt_value | pk");
                            Console.WriteLine("----+------------------+---------+---------+------------+---");
                            while (await reader.ReadAsync())
                            {
                                Console.WriteLine($"{reader["cid"],-3} | {reader["name"],-16} | {reader["type"],-7} | {reader["notnull"],-7} | {reader["dflt_value"] ?? "NULL",-10} | {reader["pk"]}");
                            }
                        }
                    }

                    Console.WriteLine($"DatabaseInitializer: Initialized tables at {fullDbPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DatabaseInitializer: Error initializing database: {ex.Message}");
                throw; // Передаём исключение дальше для отладки
            }
        }
    

// Статический метод для упрощённого вызова (без await, если нужно)
static async Task M2ain(string[] args)
        {
            string dbPath = "Db\\Notification\\DbFissure\\Fissure.db";
            Initialize(dbPath).GetAwaiter().GetResult();
        }
    }
}