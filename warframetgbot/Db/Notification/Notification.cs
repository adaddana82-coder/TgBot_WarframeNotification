using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

namespace warframetgbot.Db.Notification
{
    public static class DatabaseInitializer
    {
        public static async Task I2nitialize(string dbPath = "Db\\warframe_bot.db")
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

                    // Удаляем таблицу Users, если она существует (заменил warframe_bot на Users)
                    string dropTableQuery = "DROP TABLE IF EXISTS Users";
                    using (var command = new SQLiteCommand(dropTableQuery, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                        Console.WriteLine($"DatabaseInitializer: Dropped table Users if it existed");
                    }

                    // Создаём таблицу Users
                    var createTableQuery = new SQLiteCommand(@"
                        CREATE TABLE IF NOT EXISTS Users (
                            UserId INTEGER PRIMARY KEY,
                            IsSubscribed INTEGER NOT NULL DEFAULT 0,
                            EarthEnabled INTEGER NOT NULL DEFAULT 0,
                            CetusEnabled INTEGER NOT NULL DEFAULT 0,
                            DeimosEnabled INTEGER NOT NULL DEFAULT 0,
                            VenusEnabled INTEGER NOT NULL DEFAULT 0,
                            LastNotifiedState TEXT,
                            ArchonHuntEnabled INTEGER DEFAULT 0,
                            SortieEnabled INTEGER DEFAULT 0,
                            FissureEnabled INTEGER DEFAULT 0
                        )", connection);
                    //                            RelicIsHardEnabled INTEGER DEFAULT 0

                    await createTableQuery.ExecuteNonQueryAsync();
                    Console.WriteLine($"DatabaseInitializer: Initialized table Users at {fullDbPath}");
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
            string dbPath = "Db\\warframe_bot.db";
            I2nitialize(dbPath).GetAwaiter().GetResult();
        }
    }
}