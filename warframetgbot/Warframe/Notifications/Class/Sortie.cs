using Newtonsoft.Json.Linq;
using System.Data.SQLite;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using warframetgbot.Warframe.Notifications.Reward;

namespace warframetgbot.Warframe.Notifications.Class
{
    public class Sortie
    {
        private readonly HttpClient _httpClient;
        private readonly ITelegramBotClient _botClient;
        private readonly string _dbPath;
        private readonly JObject _sortieData;
        private readonly object _lock;
        private readonly string _ApiUrl = "https://api.warframestat.us/pc?language=ru";

        public Sortie(HttpClient httpClient, ITelegramBotClient botClient, string dbPath, JObject sortieData, object lockObject)
        {
            _httpClient = httpClient;
            _botClient = botClient;
            _dbPath = dbPath;
            _sortieData = sortieData;
            _lock = lockObject;
        }

        public async Task UpdateSortieDataAndNotifyAsync(CancellationToken cancellationToken, TimeSpan sortieTargetTime)
        {
            Console.WriteLine("Sortie: Starting UpdateSortieAndNotifyAsync");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    DateTime now = DateTime.UtcNow;
                    DateTime localTime = now.AddHours(3); // EEST = UTC+3
                    // Для теста: окно ±1 минуты
                    if (
                        localTime.Hour == sortieTargetTime.Hours &&
                        Math.Abs(localTime.Minute - sortieTargetTime.Minutes) <= 0)
                    {
                        Console.WriteLine("Sortie: Sortie time condition met, updating data and sending notifications");

                        // Обновляем данные с retry-логикой
                        string response = null;
                        int maxRetries = 3;
                        for (int i = 0; i < maxRetries; i++)
                        {
                            try
                            {
                                response = await _httpClient.GetStringAsync(_ApiUrl);
                                break;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"StatusManager: Retry {i + 1}/{maxRetries} failed for Sortie API: {ex.Message}");
                                if (i == maxRetries - 1) throw;
                                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                            }
                        }

                        var data = JObject.Parse(response);
                        lock (_lock)
                        {
                            _sortieData.RemoveAll();
                            _sortieData.Merge(data);
                        }
                        Console.WriteLine("Sortie: Updated Sortie data");

                        // Отправляем уведомления
                        var userIds = await GetSubscribedUsersAsync();
                        Console.WriteLine($"Sortie: Found {userIds.Length} users for Sortie notifications");
                        foreach (var userId in userIds)
                        {
                            var (_, _, _, _, _, _, _, sortieEnabled, _) = await GetUserSettingsAsync(userId);
                            Console.WriteLine($"Sortie: Checking SortieEnabled for user {userId}: {sortieEnabled}");
                            if (!sortieEnabled)
                            {
                                Console.WriteLine($"Sortie: Sortie notifications disabled for user {userId}");
                                continue;
                            }

                            try
                            {
                                var sortie = _sortieData["sortie"];
                                if (sortie == null || sortie["missions"] == null)
                                {
                                    Console.WriteLine($"Sortie: Sortie data not found for user {userId}");
                                    continue;
                                }

                                string faction = sortie["faction"]?.ToString() ?? "Неизвестно";
                                string eta = sortie["eta"]?.ToString() ?? "Неизвестно";
                                var missions = sortie["variants"] as JArray;

                                string message =
                                    "🔥 <i>Новая Вылазка началась!</i>🔥\n\n" +
                                   $"⚔️ <b>Фракция:</b> <i>{faction}</i>\n" +
                                   $"⏳ <b>Время до окончания:</b> <i>{eta}</i>\n\n" +
                                    "📜 <b>Миссии:</b>\n";
                                message += "<blockquote>";
                                foreach (var mission in missions)
                                {
                                    string node = mission["node"]?.ToString() ?? "Неизвестно";
                                    string missionType = mission["missionType"]?.ToString() ?? "Неизвестно";
                                    string modifier = mission["modifier"]?.ToString() ?? "Нет модификатора";

                                    // Подбираем эмодзи в зависимости от типа миссии
                                    string emoji = missionType switch
                                    {
                                        "Шпионаж" => "🕵️‍♂️",
                                        "Перехват" => "🎯",
                                        "Убийство" => "💀",
                                        "Оборона" => "🛡️",
                                        "Выживание" => "☠️",
                                        _ => "🔹"
                                    };

                                    message += $"{emoji} <b>{missionType}</b> на {node} (Мод: <i>{modifier})</i>\n";

                                }
                                message += "</blockquote>";
                                message += "<b>💎Награды:</b>\n";
                                message += "<blockquote>";

                                message += "\n<b>🥉Обычные награды:</b>\n";

                                foreach (string reward in SortieReward.CommonRewards)
                                {
                                    message += reward + "\n";

                                }
                                message += "\n<b>🥈Необычные награды:</b>\n";

                                foreach (string reward in SortieReward.UncommonRewards)
                                {
                                    message += reward + "\n";

                                }
                                message += "\n<b>🥇Редкие награды:</b>\n";

                                foreach (string reward in SortieReward.RareRewards)
                                {
                                    message += reward + "\n";

                                }
                                message += "</blockquote>";
                                await _botClient.SendTextMessageAsync(userId, message, parseMode: ParseMode.Html, cancellationToken: CancellationToken.None);
                                Console.WriteLine($"Sortie: Sent Sortie notification to user {userId}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Sortie: Error sending Sortie notification to user {userId}: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Sortie: Sortie time condition not met");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Sortie: Error in UpdateSortieDataAndNotifyAsync: {ex.Message}");
                }
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
            Console.WriteLine("Sortie: UpdateSortieDataAndNotifyAsync stopped");
        }

        private async Task<long[]> GetSubscribedUsersAsync()
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath}"))
            {
                await connection.OpenAsync();
                using (var command = new SQLiteCommand("SELECT UserId FROM Users WHERE IsSubscribed = 1 OR ArchonHuntEnabled = 1", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var userIds = new List<long>();
                        while (await reader.ReadAsync())
                        {
                            userIds.Add(reader.GetInt64(0));
                        }
                        Console.WriteLine($"Sortie: Found {userIds.Count} subscribed users");
                        return userIds.ToArray();
                    }
                }
            }
        }

        private async Task<(
            bool IsSubscribed,
            bool EarthEnabled,
            bool CetusEnabled,
            bool DeimosEnabled,
            bool VenusEnabled,
            string LastNotifiedState,
            bool ArchonHuntEnabled,
            bool SortieEnabled,
            bool FissureEnabled
            )> GetUserSettingsAsync(long userId)
        {
            using (var connection = new SQLiteConnection($"Data Source={_dbPath}"))
            {
                await connection.OpenAsync();
                object countResult = await DatabaseHelper.ExecuteScalarAsync(connection, "SELECT COUNT(*) FROM Users WHERE UserId = @UserId", ("@UserId", userId));
                long count = Convert.ToInt64(countResult);
                if (count == 0)
                {
                    Console.WriteLine($"Sortie: No settings found for user {userId}, returning defaults");
                    return (false, false, false, false, false, null, false, false, false);
                }

                using (var command = new SQLiteCommand("SELECT IsSubscribed, EarthEnabled, CetusEnabled, DeimosEnabled, VenusEnabled, LastNotifiedState, ArchonHuntEnabled, SortieEnabled, FissureEnabled FROM Users WHERE UserId = @UserId", connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var settings = (
                                IsSubscribed: reader.GetInt32(0) == 1,
                                EarthEnabled: reader.GetInt32(1) == 1,
                                CetusEnabled: reader.GetInt32(2) == 1,
                                DeimosEnabled: reader.GetInt32(3) == 1,
                                VenusEnabled: reader.GetInt32(4) == 1,
                                LastNotifiedState: reader.IsDBNull(5) ? null : reader.GetString(5),
                                ArchonHuntEnabled: reader.GetInt32(6) == 1,
                                SortieEnabled: reader.GetInt32(7) == 1,
                                FissureEnabled: reader.GetInt32(8) == 1
                            );
                            Console.WriteLine($"Sortie: Retrieved settings for user {userId}: {settings}");
                            return settings;
                        }
                    }
                }
                return (false, false, false, false, false, null, false, false, false);
            }
        }
    }
}