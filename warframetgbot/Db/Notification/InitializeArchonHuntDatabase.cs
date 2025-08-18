using System;
using System.Data.SQLite;
using System.Threading.Tasks;

class InitializeArchonHuntDatabase
{
    static async Task M2ain(string[] args)
    {
        try
        {
            // Создаём или открываем SQLite базу данных
            string dbPath = "Db\\ArchonHuntRewards.db";
            using var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            await connection.OpenAsync();

            // Удаляем старую таблицу, если она существует
            string dropTableQuery = "DROP TABLE IF EXISTS ArchonHuntRewards";
            using (var command = new SQLiteCommand(dropTableQuery, connection))
            {
                await command.ExecuteNonQueryAsync();
            }

            // Создаём таблицу ArchonHuntRewards с новым столбцом RarityOrder
            string createTableQuery = @"
                CREATE TABLE ArchonHuntRewards (
                    Boss TEXT,
                    ArchonShard TEXT,
                    Reward TEXT,
                    Rarity TEXT,
                    RarityOrder INTEGER,
                    PRIMARY KEY (Boss, Reward)
                )";
            using (var command = new SQLiteCommand(createTableQuery, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            // Данные для заполнения таблицы
            var rewardsData = new[]
           {
                new
                {
                    Boss = "Архонт Бореаль",
                    ArchonShard = "Лазурный осколок Архонта (Синий)",
                    CommonRewards = new[] {
                        "Эндо (4000)",
                        "Скульптура Анаса",
                        "Кува (6000)"
                    },
                    UncommonRewards = new[] {
                        "Ривен-мод (Винтовка, Пистолет, Дробовик, Ближний бой)",
                        "Чертеж Формы",
                        "Усилитель на 3 дня (Синтез, Ресурсы, Моды)"
                    },
                    RareRewards = new[] {
                        "Чертеж Адаптера Экзилус",
                        "Чертеж Орокин Катализатора",
                        "Чертеж Орокин Реактора",
                        "Легендарное ядро" }
                },
                new
                {
                    Boss = "Архонт Амар",
                    ArchonShard = "Багровый осколок Архонта (Красный)",
                    CommonRewards = new[] {
                        "Эндо (4000)",
                        "Скульптура Анаса",
                        "Кува (6000)"
                    },
                    UncommonRewards = new[] {
                        "Ривен-мод (Винтовка, Пистолет, Дробовик, Ближний бой)",
                        "Чертеж Формы",
                        "Усилитель на 3 дня (Синтез, Ресурсы, Моды)"
                    },
                    RareRewards = new[] {
                        "Чертеж Адаптера Экзилус",
                        "Чертеж Орокин Катализатора",
                        "Чертеж Орокин Реактора",
                        "Легендарное ядро" }
                },
                new
                {
                    Boss = "Архонт Нира",
                    ArchonShard = "Янтарный осколок Архонта (Жёлтый)",
                    CommonRewards = new[] {
                        "Эндо (4000)",
                        "Скульптура Анаса",
                        "Кува (6000)"
                    },
                    UncommonRewards = new[] {
                        "Ривен-мод (Винтовка, Пистолет, Дробовик, Ближний бой)",
                        "Чертеж Формы",
                        "Усилитель на 3 дня (Синтез, Ресурсы, Моды)"
                    },
                    RareRewards = new[] {
                        "Чертеж Адаптера Экзилус",
                        "Чертеж Орокин Катализатора",
                        "Чертеж Орокин Реактора",
                        "Легендарное ядро" }
                }
            };


            string insertQuery = @"
                INSERT OR REPLACE INTO ArchonHuntRewards (Boss, ArchonShard, Reward, Rarity, RarityOrder)
                VALUES (@Boss, @ArchonShard, @Reward, @Rarity, @RarityOrder)";
            foreach (var rewardSet in rewardsData)
            {

                // Вставляем обычные награды
                foreach (var reward in rewardSet.CommonRewards)
                {
                    using (var command = new SQLiteCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Boss", rewardSet.Boss);
                        command.Parameters.AddWithValue("@ArchonShard", rewardSet.ArchonShard);
                        command.Parameters.AddWithValue("@Reward", reward);
                        command.Parameters.AddWithValue("@Rarity", "Common");
                        command.Parameters.AddWithValue("@RarityOrder", 1);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Вставляем необычные награды
                foreach (var reward in rewardSet.UncommonRewards)
                {
                    using (var command = new SQLiteCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Boss", rewardSet.Boss);
                        command.Parameters.AddWithValue("@ArchonShard", rewardSet.ArchonShard);
                        command.Parameters.AddWithValue("@Reward", reward);
                        command.Parameters.AddWithValue("@Rarity", "Uncommon");
                        command.Parameters.AddWithValue("@RarityOrder", 2);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Вставляем редкие награды
                foreach (var reward in rewardSet.RareRewards)
                {
                    using (var command = new SQLiteCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Boss", rewardSet.Boss);
                        command.Parameters.AddWithValue("@ArchonShard", rewardSet.ArchonShard);
                        command.Parameters.AddWithValue("@Reward", reward);
                        command.Parameters.AddWithValue("@Rarity", "Rare");
                        command.Parameters.AddWithValue("@RarityOrder", 3);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }

            // Проверяем содержимое таблицы
            Console.WriteLine("Таблица ArchonHuntRewards успешно создана и заполнена:");
            string selectQuery = "SELECT * FROM ArchonHuntRewards ORDER BY Boss, RarityOrder, Reward";
            using (var command = new SQLiteCommand(selectQuery, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"Босс: {reader["Boss"]}, Осколок: {reader["ArchonShard"]}, Награда: {reader["Reward"]}, Редкость: {reader["Rarity"]}, Порядок: {reader["RarityOrder"]}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Произошла ошибка: {ex.Message}");
        }
    }
}