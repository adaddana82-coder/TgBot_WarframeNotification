using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace warframetgbot.Warframe.Notifications.Class
{
    public  class Status
    {
        private static ITelegramBotClient _botClient;
        private static Manager _statusManager;
        private static readonly Dictionary<string, DateTime> _notificationFlags = new(); // Хранит время последней отправки уведомления
        private readonly object _lock = new object();
        private readonly HttpClient _httpClient;
        private readonly string _ApiUrl = "https://api.warframestat.us/pc?language=ru";
        private readonly string _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Db", "warframe_bot.db");
        private JObject _statusData;
        public Status(HttpClient httpClient, ITelegramBotClient botClient, string dbPath, JObject  statusData, object lockObject)
        {
            _httpClient = httpClient;
            _botClient = botClient;
            _dbPath = dbPath;
            _statusData = _statusData;
            _lock = lockObject;
        }
              public static void Initialize(ITelegramBotClient botClient, Manager statusManager)
        {
            _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
            _statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
            Task.Run(() => SendNotificationsAsync(CancellationToken.None)).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Console.WriteLine($"Status: SendNotificationsAsync failed: {t.Exception?.InnerException?.Message}");
            });
            Console.WriteLine("Status: Initialized");
        }
        public async Task UpdateStatusDataAsync(CancellationToken cancellationToken, TimeSpan _statusUpdateInterval)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    string response = await _httpClient.GetStringAsync(_ApiUrl);
                    var data = JObject.Parse(response);
                    lock (_lock)
                    {
                        _statusData = data;
                    }
                    Console.WriteLine("StatusManager: Updated Status data");
                    await Task.Delay(_statusUpdateInterval, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"StatusManager: Error updating Status data: {ex.Message}");
                    await Task.Delay(_statusUpdateInterval, cancellationToken);
                }
            }
        }
        private static async Task SendNotificationsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("Status: Sending notifications...");
                    var userIds = await _statusManager.GetSubscribedUsersAsync();
                    Console.WriteLine($"Status: Processing {userIds.Length} subscribed users");

                    foreach (var userId in userIds)
                    {
                        Console.WriteLine($"Status: Processing user {userId}");
                        var (isSubscribed, earthEnabled, cetusEnabled, deimosEnabled, venusEnabled, lastNotifiedState, archonHuntEnabled, sortieEnabled, fissureEnabled) = await _statusManager.GetUserSettingsAsync(userId);
                        if (!isSubscribed)
                        {
                            Console.WriteLine($"Status: User {userId} is not subscribed, skipping");
                            continue;
                        }

                        var statusData = _statusManager.GetStatusData();
                        var earth = statusData["earthCycle"];
                        var cetus = statusData["cetusCycle"];
                        var vallis = statusData["vallisCycle"];
                        var cambion = statusData["cambionCycle"];
                        bool shouldUpdateSettings = false;
                        string newLastNotifiedState = lastNotifiedState;

                        // Текущее время в EEST (UTC+3)
                        DateTime now = DateTime.UtcNow.AddHours(3);
                        TimeSpan notificationWindow = TimeSpan.FromMinutes(0); // Окно ±1 минута
                        TimeSpan notificationOffset = TimeSpan.FromMinutes(15); // Уведомление за 15 минут

                        if (earthEnabled && earth != null)
                        {
                            var (updateSettings, updatedState) = await HandleCycleNotification(userId, "earth", earth, "state", now, notificationWindow, notificationOffset, cancellationToken, new[] { "day", "night" });
                            shouldUpdateSettings |= updateSettings;
                            if (updatedState != null) newLastNotifiedState = updatedState;
                        }

                        if (cetusEnabled && cetus != null)
                        {
                            var (updateSettings, updatedState) = await HandleCycleNotification(userId, "cetus", cetus, "state", now, notificationWindow, notificationOffset, cancellationToken, new[] { "day", "night" });
                            shouldUpdateSettings |= updateSettings;
                            if (updatedState != null) newLastNotifiedState = updatedState;
                        }

                        if (deimosEnabled && cambion != null)
                        {
                            var (updateSettings, updatedState) = await HandleCycleNotification(userId, "cambion", cambion, "active", now, notificationWindow, notificationOffset, cancellationToken, new[] { "fass", "vome" });
                            shouldUpdateSettings |= updateSettings;
                            if (updatedState != null) newLastNotifiedState = updatedState;
                        }

                        if (venusEnabled && vallis != null)
                        {
                            var (updateSettings, updatedState) = await HandleCycleNotification(userId, "vallis", vallis, "state", now, notificationWindow, notificationOffset, cancellationToken, new[] { "warm", "cold" });
                            shouldUpdateSettings |= updateSettings;
                            if (updatedState != null) newLastNotifiedState = updatedState;
                        }

                        if (shouldUpdateSettings)
                        {
                            Console.WriteLine($"Status: Updating LastNotifiedState for user {userId} to {newLastNotifiedState}, preserving ArchonHuntEnabled={archonHuntEnabled}");
                            await _statusManager.UpdateUserSettingsAsync(
                                userId,
                                isSubscribed,
                                earthEnabled,
                                cetusEnabled,
                                deimosEnabled,
                                venusEnabled,
                                newLastNotifiedState,
                                archonHuntEnabled,
                                sortieEnabled,
                                fissureEnabled
                            );
                        }

                        Console.WriteLine($"Status: Processed notifications for user {userId}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Status: Error in SendNotificationsAsync: {ex.Message}");
                }
                await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
            }
        }

        private static async Task<(bool shouldUpdateSettings, string newLastNotifiedState)> HandleCycleNotification(
            long userId,
            string cycle,
            JToken cycleData,
            string stateKey,
            DateTime now,
            TimeSpan notificationWindow,
            TimeSpan notificationOffset,
            CancellationToken cancellationToken,
            string[] validStates)
        {
            string currentState = cycleData[stateKey]?.ToString();
            string expiryStr = cycleData["expiry"]?.ToString();

            if (string.IsNullOrEmpty(currentState) || string.IsNullOrEmpty(expiryStr))
            {
                Console.WriteLine($"Status: Invalid data for {cycle} for user {userId}, state={currentState}, expiry={expiryStr}");
                return (false, null);
            }

            if (!DateTime.TryParse(expiryStr, out DateTime expiryUtc))
            {
                Console.WriteLine($"Status: Failed to parse expiry time for {cycle} for user {userId}: {expiryStr}");
                return (false, null);
            }

            DateTime expiry = expiryUtc.AddHours(3); // EEST = UTC+3
            DateTime notificationTime = expiry - notificationOffset; // Время уведомления (за 15 минут)
            string nextState = GetNextState(currentState, validStates);
            string key = $"{userId}_{cycle}";

            Console.WriteLine($"Status: Checking {cycle} for user {userId}: currentState={currentState}, expiry={expiry:yyyy-MM-dd HH:mm:ss}, notificationTime={notificationTime:yyyy-MM-dd HH:mm:ss}, now={now:yyyy-MM-dd HH:mm:ss}");

            // Проверяем, находится ли текущее время в окне уведомления (±1 минута от времени за 15 минут до expiry)
            if (Math.Abs((now - notificationTime).TotalMinutes) <= notificationWindow.TotalMinutes)
            {
                // Проверяем, отправляли ли уже уведомление для этого цикла
                if (_notificationFlags.TryGetValue(key, out DateTime lastSentTime) &&
                    now.Date == lastSentTime.Date &&
                    Math.Abs((now - lastSentTime).TotalMinutes) < notificationOffset.TotalMinutes)
                {
                    Console.WriteLine($"Status: Skipping notification for {cycle} for user {userId}, already sent at {lastSentTime:yyyy-MM-dd HH:mm:ss}");
                    return (false, null);
                }

                // Отправляем уведомление о следующем состоянии
                string message = GetNotificationMessage(cycle, nextState);
                await _botClient.SendTextMessageAsync(userId, message, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
                _notificationFlags[key] = now;
                Console.WriteLine($"Status: Sent notification for {cycle} to user {userId}, next state: {nextState} at {now:yyyy-MM-dd HH:mm:ss}");

                // Обновляем LastNotifiedState только для Венеры
                if (cycle == "vallis")
                {
                    return (true, nextState);
                }
                return (false, null);
            }
            else if ((now - expiry).TotalMinutes > 0)
            {
                // Сбрасываем флаг, если цикл завершился
                if (_notificationFlags.ContainsKey(key))
                {
                    Console.WriteLine($"Status: Cycle {cycle} expired for user {userId}, resetting notification flag");
                    _notificationFlags.Remove(key);
                }
            }

            return (false, null);
        }

        private static string GetNextState(string currentState, string[] validStates)
        {
            if (currentState == validStates[0])
                return validStates[1];
            return validStates[0];
        }

        private static string GetNotificationMessage(string cycle, string nextState)
        {
            return cycle switch
            {
                "earth" => $"🌍 <b>Земля:</b> через <i>15</i> минут начнётся {nextState}",
                "cetus" => $"🛕 <b>Цетус:</b> через <i>15</i>  минут начнётся {nextState}",
                "cambion" => $"🫀 <b>Деймос:</b> через <i>15</i> минут начнётся {nextState}",
                "vallis" => $"🏭 <b>Венера:</b> через <i>15</i> минут начнётся {nextState}",
                _ => $"Неизвестный цикл: через 15 минут начнётся {nextState}"
            };
        }
    }
}