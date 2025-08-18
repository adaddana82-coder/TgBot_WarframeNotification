using Newtonsoft.Json.Linq;
using Polly;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using warframetgbot.Warframe;
using warframetgbot.Warframe.Notifications.Reward;

public class Fissure
{
    public long Id { get; set; }
    public string Node { get; set; }
    public string MissionType { get; set; }
    public string Tier { get; set; }
    public string Enemy { get; set; }
    public string Eta { get; set; }
    public bool IsHard { get; set; }
    public int TierNum { get; set; }
    public DateTime Timestamp { get; set; }

    private static ITelegramBotClient _botClient;
    private static Manager _fissureManager;
    private static readonly Dictionary<string, DateTime> _notificationFlags = new(); // Хранит время последней отправки уведомления
    private readonly object _lock = new object();
    private readonly HttpClient _httpClient;
    private readonly string _ApiUrl = "https://api.warframestat.us/pc?language=ru";
    private readonly string _dbPath;
    private JObject _fissureData;
    public Fissure()
    { }

    public Fissure(HttpClient httpClient, ITelegramBotClient botClient, string dbPath, JObject statusData, object lockObject)
    {
        _httpClient = httpClient;
        _botClient = botClient;
        _dbPath = dbPath;
        _fissureData = statusData; // Исправил опечатку
        _lock = lockObject;
    }

    public async Task UpdateAndNotifyAsync(CancellationToken cancellationToken, TimeSpan _fissureUpdateInterval)
    {
        Console.WriteLine("Fissure: Initialized.");
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var userSettingsDict = await GetSubscribedUsersWithSettingsAsync();
                try
                {
                    var policy = Policy
                        .Handle<HttpRequestException>()
                        .Or<TaskCanceledException>()
                        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                            (exception, timeSpan, retryCount, context) =>
                            {
                                Console.WriteLine($"Fissure: Попытка {retryCount} не удалась: {exception.Message}. Повтор через {timeSpan.TotalSeconds} сек.");
                            });

                    await policy.ExecuteAsync(async () =>
                    {
                        string url = "https://api.warframestat.us/pc";
                        HttpResponseMessage response = await _httpClient.GetAsync(url); // Исправил на _httpClient

                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Ошибка запроса: {response.StatusCode}");
                            return;
                        }

                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        JObject data = JObject.Parse(jsonResponse);

                        if (data["fissures"] == null || data["fissures"].Type != JTokenType.Array)
                        {
                            Console.WriteLine("Fissure: Данные о разломах отсутствуют или имеют неверный формат.");
                            return;
                        }

                        JArray fissures = (JArray)data["fissures"];
                        if (fissures.Count == 0)
                        {
                            Console.WriteLine("Fissure:Активные разломы не найдены.");
                            return;
                        }

                        var nonStormFissures = fissures.Where(f => !(bool)f["isStorm"]).Select(FromJToken).ToList();
                        var storedFissures = await LoadFissuresFromDatabase();
                        var newFissures = nonStormFissures.Except(storedFissures, new FissureComparer()).ToList();
                        Console.WriteLine($"Fissure: Found {newFissures.Count} new fissures from API.");
                        foreach (var f in newFissures)
                        {
                            Console.WriteLine($"Fissure: New fissure details: Node='{f.Node}', MissionType='{f.MissionType}', Tier='{f.Tier}', IsHard={f.IsHard}, Eta='{f.Eta}'");
                        }
                        Console.WriteLine($"Fissure: Processing {userSettingsDict.Count} users with settings.");
                        foreach (var kvp in userSettingsDict)
                        {
                            long userId = kvp.Key;
                            UserSettings settings = kvp.Value;

                            // Фильтруем все newFissures по settings юзера
                            var userNewFissures = newFissures.Where(f => MatchesUserSettings(f, settings)).ToList();

                            if (userNewFissures.Count == 0)
                                continue;  // Ничего не подходит — пропускаем

                            // Разделяем на hard и non-hard для сообщений
                            var falseHardForUser = userNewFissures.Where(f => !f.IsHard).OrderBy(f => f.TierNum).ToList();
                            var trueHardForUser = userNewFissures.Where(f => f.IsHard).OrderBy(f => f.TierNum).ToList();

                            // Отправка для non-hard
                            foreach (var fissure in falseHardForUser)
                            {
                                string emoji = fissure.MissionType switch
                                {
                                    "Шпионаж" => "🕵️‍♂️",
                                    "Перехват" => "🎯",
                                    "Убийство" => "💀",
                                    "Оборона" => "🛡️",
                                    "Выживание" => "☠️",
                                    _ => "🔹"
                                };
                                string message =
                                          " 🔥<b>Новый разрыв Бездны</b>🔥\n\n" +
                                          "<blockquote>" +
                                          "=================================\n" +
                                          $"🧭 <b>Локация:</b> {fissure.Node}\n" +
                                          $"📜 <b>Миссия:</b>  {fissure.MissionType}{emoji}\n" +
                                          $"🌟 <b>Тир:</b> {fissure.Tier}\n" +
                                          $"⚔️ <b>Фракция:</b> {fissure.Enemy}\n" +
                                          $"⏳ <b>Время до окончания:</b> {fissure.Eta}\n\n" +
                                          "=================================\n" +
                                          "</blockquote>";
                                await _botClient.SendTextMessageAsync(userId, message, parseMode: ParseMode.Html, cancellationToken: CancellationToken.None);
                                Console.WriteLine($"Fissure: Sent non-hard Fissure notification to user {userId}");
                            }

                            // Отправка для hard
                            foreach (var fissure in trueHardForUser)
                            {
                                string emoji = fissure.MissionType switch
                                {
                                    "Шпионаж" => "🕵️‍♂️",
                                    "Перехват" => "🎯",
                                    "Убийство" => "💀",
                                    "Оборона" => "🛡️",
                                    "Выживание" => "☠️",
                                    _ => "🔹"
                                };
                                string message =
                                          "🔥<i>Новый разрыв Бездны (Стальной Путь)</i>🔥\n\n" +
                                           "<blockquote>" +
                                          "=================================\n" +
                                          $"🧭 <b>Локация:</b> {fissure.Node}\n" +
                                          $"📜 <b>Миссия:</b>  {fissure.MissionType}{emoji}\n" +
                                          $"🌟 <b>Тир:</b> {fissure.Tier}\n" +
                                          $"⚔️ <b>Фракция:</b> {fissure.Enemy}\n" +
                                          $"⏳ <b>Время до окончания:</b> {fissure.Eta}\n\n" +
                                          "=================================\n" +
                                          "</blockquote>";
                                await _botClient.SendTextMessageAsync(userId, message, parseMode: ParseMode.Html, cancellationToken: CancellationToken.None);
                                Console.WriteLine($"Fissure: Sent hard Fissure notification to user {userId}");
                            }
                        }
                        await CleanOldFissures();
                        await SaveFissuresToDatabase(nonStormFissures);
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fissure: Критическая ошибка: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Fissure: Внутренняя ошибка: {ex.InnerException.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StatusManager: Error in UpdateFissureDataAsync: {ex.Message}");
            }
            await Task.Delay(_fissureUpdateInterval, cancellationToken);
        }
    }

    private async Task<Dictionary<long, UserSettings>> GetSubscribedUsersWithSettingsAsync()
    {
        var userSettings = new Dictionary<long, UserSettings>();
        using (var connection = new SQLiteConnection($"Data Source={_dbPath}"))
        {
            await connection.OpenAsync();
            using (var command = new SQLiteCommand(@"
                SELECT UserId, IsHardEnabled, PlanetEnabled, TierEnabled, MissionEnabled 
                FROM Users 
                WHERE IsHardEnabled != '' OR PlanetEnabled != '' OR TierEnabled != '' OR MissionEnabled != ''", connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        long userId = reader.GetInt64(0);
                        string hardStr = reader.GetString(1).Trim();
                        string planetStr = reader.GetString(2).Trim();
                        string tierStr = reader.GetString(3).Trim();
                        string missionStr = reader.GetString(4).Trim();

                        var settings = new UserSettings
                        {
                            HardModes = string.IsNullOrEmpty(hardStr) ? new List<string>() : hardStr.Split(',').Select(s => s.Trim()).ToList(),
                            Planets = string.IsNullOrEmpty(planetStr) ? new List<string>() : planetStr.Split(',').Select(s => s.Trim()).ToList(),
                            Tiers = string.IsNullOrEmpty(tierStr) ? new List<string>() : tierStr.Split(',').Select(s => s.Trim()).ToList(),
                            Missions = string.IsNullOrEmpty(missionStr) ? new List<string>() : missionStr.Split(',').Select(s => s.Trim()).ToList()
                        };
                        Console.WriteLine($"Fissure: Parsing settings for user {userId}: Raw Hard='{hardStr}', Planets='{planetStr}', Tiers='{tierStr}', Missions='{missionStr}'");
                        Console.WriteLine($"Fissure: Parsed lists: HardModes={string.Join(",", settings.HardModes)}, Planets={string.Join(",", settings.Planets)}, Tiers={string.Join(",", settings.Tiers)}, Missions={string.Join(",", settings.Missions)}");
                        if (settings.IsSubscribed)
                        {
                            userSettings[userId] = settings;
                            Console.WriteLine($"Fissure: Loaded settings for user {userId}: Hard={hardStr}, Planets={planetStr}, Tiers={tierStr}, Missions={missionStr}");
                        }
                    }
                }
            }
        }
        Console.WriteLine($"Fissure: Found {userSettings.Count} subscribed users with settings");
        return userSettings;
    }

    public Fissure FromJToken(JToken token)
    {
        return new Fissure
        {
            Node = (string)token["node"],
            MissionType = (string)token["missionType"],
            Tier = (string)token["tier"],
            Enemy = (string)token["enemy"],
            Eta = (string)token["eta"],
            IsHard = (bool)token["isHard"],
            TierNum = (int)token["tierNum"],
            Timestamp = DateTime.UtcNow
        };
    }

    private readonly HttpClient client = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30)
    };
    private static readonly string connectionString = $"Data Source=Db\\Notification\\DbFissure\\Fissure.db;Version=3;";

    public async Task<List<Fissure>> LoadFissuresFromDatabase()
    {
        var fissures = new List<Fissure>();
        try
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            string selectQuery = "SELECT * FROM Fissures";
            using var command = new SQLiteCommand(selectQuery, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                fissures.Add(new Fissure
                {
                    Id = reader.GetInt64(0),
                    Node = reader.GetString(1),
                    MissionType = reader.GetString(2),
                    Tier = reader.GetString(3),
                    Enemy = reader.GetString(4),
                    Eta = reader.GetString(5),
                    IsHard = reader.GetInt32(6) == 1,
                    TierNum = reader.GetInt32(7),
                    Timestamp = DateTime.Parse(reader.GetString(8))
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fissure: Ошибка загрузки данных из базы: {ex.Message}");
        }
        return fissures;
    }

    public async Task SaveFissuresToDatabase(List<Fissure> fissures)
    {
        try
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            string deleteQuery = "DELETE FROM Fissures";
            using (var command = new SQLiteCommand(deleteQuery, connection))
            {
                await command.ExecuteNonQueryAsync();
            }

            string insertQuery = @"
                INSERT INTO Fissures (Node, MissionType, Tier, Enemy, Eta, IsHard, TierNum, Timestamp)
                VALUES (@Node, @MissionType, @Tier, @Enemy, @Eta, @IsHard, @TierNum, @Timestamp)";
            foreach (var fissure in fissures)
            {
                using var command = new SQLiteCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@Node", fissure.Node);
                command.Parameters.AddWithValue("@MissionType", fissure.MissionType);
                command.Parameters.AddWithValue("@Tier", fissure.Tier);
                command.Parameters.AddWithValue("@Enemy", fissure.Enemy);
                command.Parameters.AddWithValue("@Eta", fissure.Eta);
                command.Parameters.AddWithValue("@IsHard", fissure.IsHard ? 1 : 0);
                command.Parameters.AddWithValue("@TierNum", fissure.TierNum);
                command.Parameters.AddWithValue("@Timestamp", fissure.Timestamp.ToString("o"));
                await command.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fissure: Ошибка сохранения данных в базу: {ex.Message}");
        }
    }

    public async Task CleanOldFissures()
    {
        try
        {
            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            string deleteQuery = "DELETE FROM Fissures WHERE Timestamp < @ExpiryTime";
            using var command = new SQLiteCommand(deleteQuery, connection);
            command.Parameters.AddWithValue("@ExpiryTime", DateTime.UtcNow.AddHours(-24).ToString("o"));
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fissure: Ошибка очистки старых данных: {ex.Message}");
        }
    }

    private bool MatchesUserSettings(Fissure fissure, UserSettings settings)
    {
        // Если фильтр пуст — он проходит (игнорируем)

        // Hard: "True" для hard, "False" для non-hard
        if (settings.HardModes.Any())
        {
            string requiredHard = fissure.IsHard ? "true" : "false";
            if (!settings.HardModes.Contains(requiredHard))
                return false;
        }

        // Planet: Парсим планету из Node (например, "Earth (Coba)" -> "Earth")
        if (settings.Planets.Any())
        {
            string planet = fissure.Node.Split(' ')[0].Trim();  // Простой парс, assuming формат "Planet (Node)"
            if (!settings.Planets.Contains(planet, StringComparer.OrdinalIgnoreCase))  // Игнор case
                return false;
        }

        // Tier: Прямое совпадение
        if (settings.Tiers.Any())
        {
            if (!settings.Tiers.Contains(fissure.Tier, StringComparer.OrdinalIgnoreCase))
                return false;
        }

        // Mission: Прямое совпадение
        if (settings.Missions.Any())
        {
            if (!settings.Missions.Contains(fissure.MissionType, StringComparer.OrdinalIgnoreCase))
                return false;
        }

        return true;  // Если все фильтры прошли или пусты
    }
}

public class FissureComparer : IEqualityComparer<Fissure>
{
    public bool Equals(Fissure x, Fissure y)
    {
        if (x == null || y == null) return false;
        return x.Node == y.Node && x.MissionType == y.MissionType && x.Tier == y.Tier;
    }

    public int GetHashCode(Fissure obj)
    {
        return (obj.Node + obj.MissionType + obj.Tier).GetHashCode();
    }
}

public class UserSettings
{
    public List<string> HardModes { get; set; } = new List<string>();  // "True" или "False"
    public List<string> Planets { get; set; } = new List<string>();
    public List<string> Tiers { get; set; } = new List<string>();
    public List<string> Missions { get; set; } = new List<string>();

    public bool IsSubscribed => HardModes.Any() || Planets.Any() || Tiers.Any() || Missions.Any();  // Если хоть один список не пуст — subscribed
}