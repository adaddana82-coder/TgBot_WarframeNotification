using Newtonsoft.Json.Linq;
using System.Data.SQLite;
using Telegram.Bot;
using warframetgbot.Warframe.Notifications.Class;

namespace warframetgbot.Warframe
{
    public class Manager
    {
        private readonly string _ApiUrl = "https://api.warframestat.us/pc?language=ru";
        private readonly string _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Db", "warframe_bot.db");
        private readonly string _dbPathFisssure = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Db\\Notification\\DbFissure\\Fissure.db");
        private readonly HttpClient _httpClient;
        private readonly ITelegramBotClient _botClient;
        private readonly object _lock = new object();
        private JObject _statusData;
        private JObject _archonHuntData;
        private JObject _sortieData;
        private JObject _fissureData;
        private CancellationTokenSource _cts;
        private readonly TimeSpan _statusUpdateInterval = TimeSpan.FromSeconds(60);
        private readonly DayOfWeek _archonHuntTargetDay = DayOfWeek.Monday; // Для теста
        private readonly TimeSpan _archonHuntTargetTime = new TimeSpan(6, 0, 0); // Для теста, 0:40 EEST
        private readonly TimeSpan _sortieTargetTime = new TimeSpan(20, 0, 0);
        private readonly TimeSpan _fissureUpdateInterval = TimeSpan.FromSeconds(1); // настроить время 
        private readonly Archon _archon;
        private readonly Sortie _sortie;
        private readonly Status _status;
        private readonly Fissure _fissure;
        public Manager(ITelegramBotClient botClient)
        {
            _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
            _httpClient = new HttpClient();
            _statusData = new JObject();
            _archonHuntData = new JObject();
            _sortieData = new JObject();
            _fissureData = new JObject();
            _cts = new CancellationTokenSource();
            _archon = new Archon(_httpClient, _botClient, _dbPath, _archonHuntData, _lock);
            _sortie = new Sortie(_httpClient, _botClient, _dbPath, _sortieData, _lock);
            _status = new Status(_httpClient, _botClient, _dbPath, _statusData, _lock);
            _fissure = new Fissure(_httpClient, _botClient, _dbPathFisssure, _fissureData, _lock);
            Console.WriteLine("StatusManager: Initialized");
        }


        public void Start()
        {
            Task.Run(() => _status.UpdateStatusDataAsync(_cts.Token, _statusUpdateInterval)).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Console.WriteLine($"StatusManager: UpdateStatusDataAsync failed: {t.Exception?.InnerException?.Message}");
            });
            Task.Run(() => _archon.UpdateArchonHuntDataAndNotifyAsync(_cts.Token, _archonHuntTargetDay, _archonHuntTargetTime)).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Console.WriteLine($"StatusManager: UpdateArchonHuntDataAndNotifyAsync failed: {t.Exception?.InnerException?.Message}");
            });
            Task.Run(() => _sortie.UpdateSortieDataAndNotifyAsync(_cts.Token, _sortieTargetTime)).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Console.WriteLine($"StatusManager: UpdateSortieDataAndNotifyAsync failed: {t.Exception?.InnerException?.Message}");
            });
            Task.Run(() => _fissure.UpdateAndNotifyAsync(_cts.Token, _fissureUpdateInterval)).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Console.WriteLine($"StatusManager: UpdateFissureDataAndNotifyAsync failed: {t.Exception?.InnerException?.Message}");
            });
            Console.WriteLine("StatusManager: Started periodic updates");
        }


        public void Stop()
        {
            _cts.Cancel();
            _httpClient.Dispose();
            Console.WriteLine("StatusManager: Stopped");
        }

        public JObject GetStatusData()
        {
            lock (_lock)
            {
                return _statusData;
            }
        }
        public JObject GetSortieData()
        {
            lock (_lock)
            {
                return _sortieData;
            }
        }

        public JObject GetArchonHuntData()
        {
            lock (_lock)
            {
                return _archonHuntData;
            }
        }
        public JObject GetFissureData()
        {
            lock (_lock)
            {
                return _fissureData;
            }
        }

        public async Task<(
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
            using var connection = new SQLiteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();
            object countResult = await DatabaseHelper.ExecuteScalarAsync(connection, "SELECT COUNT(*) FROM Users WHERE UserId = @UserId", ("@UserId", userId));
            long count = Convert.ToInt64(countResult);
            if (count == 0)
            {
                Console.WriteLine($"StatusManager: No settings found for user {userId}, returning defaults");
                return (false, false, false, false, false, null, false, false, false);
            }

            using (var command = new SQLiteCommand("SELECT IsSubscribed, EarthEnabled, CetusEnabled, DeimosEnabled, VenusEnabled, LastNotifiedState, ArchonHuntEnabled, SortieEnabled, FissureEnabled FROM Users WHERE UserId = @UserId", connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                using var reader = await command.ExecuteReaderAsync();
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
                    Console.WriteLine($"StatusManager: Retrieved settings for user {userId}: {settings}");
                    return settings;
                }
            }
            return (false, false, false, false, false, null, false, false, false);
        }

        public async Task UpdateUserSettingsAsync(long userId,
            bool isSubscribed,
            bool earthEnabled,
            bool cetusEnabled,
            bool deimosEnabled,
            bool venusEnabled,
            string lastNotifiedState,
            bool archonHuntEnabled,
            bool sortieEnabled,
            bool fissureEnabled)
        {
            using var connection = new SQLiteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();
            object countResult = await DatabaseHelper.ExecuteScalarAsync(connection, "SELECT COUNT(*) FROM Users WHERE UserId = @UserId", ("@UserId", userId));
            long count = Convert.ToInt64(countResult);

            try
            {
                if (count == 0)
                {
                    await DatabaseHelper.ExecuteNonQueryAsync(connection, @"
                            INSERT INTO Users (UserId, IsSubscribed, EarthEnabled, CetusEnabled, DeimosEnabled, VenusEnabled, LastNotifiedState, ArchonHuntEnabled, SortieEnabled, FissureEnabled)
                            VALUES (@UserId, @IsSubscribed, @EarthEnabled, @CetusEnabled, @DeimosEnabled, @VenusEnabled, @LastNotifiedState, @ArchonHuntEnabled, @SortieEnabled ,@FissureEnabled)",
                        ("@UserId", userId),
                        ("@IsSubscribed", isSubscribed ? 1 : 0),
                        ("@EarthEnabled", earthEnabled ? 1 : 0),
                        ("@CetusEnabled", cetusEnabled ? 1 : 0),
                        ("@DeimosEnabled", deimosEnabled ? 1 : 0),
                        ("@VenusEnabled", venusEnabled ? 1 : 0),
                        ("@LastNotifiedState", lastNotifiedState ?? (object)DBNull.Value),
                        ("@ArchonHuntEnabled", archonHuntEnabled ? 1 : 0),
                        ("@SortieEnabled", sortieEnabled ? 1 : 0),
                        ("@FissureEnabled", fissureEnabled ? 1 : 0)
                        );

                    Console.WriteLine($"StatusManager: Created new user {userId} with settings: " +
                        $"IsSubscribed={isSubscribed}," +
                        $" Earth={earthEnabled}," +
                        $" Cetus={cetusEnabled}," +
                        $" Deimos={deimosEnabled}," +
                        $" Venus={venusEnabled}," +
                        $" LastNotifiedState={lastNotifiedState}," +
                        $" ArchonHuntEnabled={archonHuntEnabled}," +
                        $" SoriteEnabled = {sortieEnabled}" +
                        $" FissureEnabled = {fissureEnabled}");
                }
                else
                {
                    await DatabaseHelper.ExecuteNonQueryAsync(connection, @"
                            UPDATE Users 
                            SET 
                                IsSubscribed = @IsSubscribed, 
                                EarthEnabled = @EarthEnabled, 
                                CetusEnabled = @CetusEnabled, 
                                DeimosEnabled = @DeimosEnabled, 
                                VenusEnabled = @VenusEnabled, 
                                LastNotifiedState = @LastNotifiedState,
                                ArchonHuntEnabled = @ArchonHuntEnabled,
                                SortieEnabled = @SortieEnabled,
                                FissureEnabled = @FissureEnabled
                            WHERE UserId = @UserId",
                        ("@UserId", userId),
                        ("@IsSubscribed", isSubscribed ? 1 : 0),
                        ("@EarthEnabled", earthEnabled ? 1 : 0),
                        ("@CetusEnabled", cetusEnabled ? 1 : 0),
                        ("@DeimosEnabled", deimosEnabled ? 1 : 0),
                        ("@VenusEnabled", venusEnabled ? 1 : 0),
                        ("@LastNotifiedState", lastNotifiedState ?? (object)DBNull.Value),
                        ("@ArchonHuntEnabled", archonHuntEnabled ? 1 : 0),
                        ("@SortieEnabled", sortieEnabled ? 1 : 0),
                        ("@FissureEnabled", fissureEnabled ? 1 : 0));

                    Console.WriteLine($"StatusManager: Updated settings for user {userId}:" +
                        $" IsSubscribed={isSubscribed}," +
                        $" Earth={earthEnabled}," +
                        $" Cetus={cetusEnabled}," +
                        $" Deimos={deimosEnabled}," +
                        $" Venus={venusEnabled}," +
                        $" LastNotifiedState={lastNotifiedState}," +
                        $" ArchonHuntEnabled={archonHuntEnabled}," +
                        $" SortieEnabled = {sortieEnabled}" +
                        $" FissureEnabled = {fissureEnabled}"
                        );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StatusManager: Error updating settings for user {userId}: {ex.Message}");
                throw; // Для отладки
            }
        }


        public async Task<long[]> GetSubscribedUsersAsync()
        {
            using var connection = new SQLiteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();
            using var command = new SQLiteCommand("SELECT UserId FROM Users WHERE IsSubscribed = 1 OR ArchonHuntEnabled = 1", connection);
            using var reader = await command.ExecuteReaderAsync();
            var userIds = new List<long>();
            while (await reader.ReadAsync())
            {
                userIds.Add(reader.GetInt64(0));
            }
            Console.WriteLine($"StatusManager: Found {userIds.Count} subscribed users");
            return userIds.ToArray();
        }
        public async Task UpdateFissureSettingsAsync(long userId, string isHardEnabled, string planetEnabled, string tierEnabled, string missionEnabled)
        {
            using var connection = new SQLiteConnection($"Data Source={_dbPathFisssure}");
            await connection.OpenAsync();
            object countResult = await DatabaseHelper.ExecuteScalarAsync(connection, "SELECT COUNT(*) FROM Users WHERE UserId = @UserId", ("@UserId", userId));
            long count = Convert.ToInt64(countResult);

            try
            {
                if (count == 0)
                {
                    await DatabaseHelper.ExecuteNonQueryAsync(connection, @"
                    INSERT INTO Users (UserId, IsHardEnabled, PlanetEnabled, TierEnabled, MissionEnabled)
                    VALUES (@UserId, @IsHardEnabled, @PlanetEnabled, @TierEnabled, @MissionEnabled)",
                        ("@UserId", userId),
                        ("@IsHardEnabled", isHardEnabled ?? (object)DBNull.Value),
                        ("@PlanetEnabled", planetEnabled ?? (object)DBNull.Value),
                        ("@TierEnabled", tierEnabled ?? (object)DBNull.Value),
                        ("@MissionEnabled", missionEnabled ?? (object)DBNull.Value)
                    );

                    Console.WriteLine($"StatusManager: Created new user {userId} with settings: " +
                        $"IsHardEnabled={isHardEnabled}, PlanetEnabled={planetEnabled}, TierEnabled={tierEnabled}, MissionEnabled={missionEnabled}");
                }
                else
                {
                    await DatabaseHelper.ExecuteNonQueryAsync(connection, @"
                    UPDATE Users 
                    SET 
                        IsHardEnabled = @IsHardEnabled, 
                        PlanetEnabled = @PlanetEnabled, 
                        TierEnabled = @TierEnabled, 
                        MissionEnabled = @MissionEnabled
                    WHERE UserId = @UserId",
                        ("@UserId", userId),
                        ("@IsHardEnabled", isHardEnabled ?? (object)DBNull.Value),
                        ("@PlanetEnabled", planetEnabled ?? (object)DBNull.Value),
                        ("@TierEnabled", tierEnabled ?? (object)DBNull.Value),
                        ("@MissionEnabled", missionEnabled ?? (object)DBNull.Value)
                    );

                    Console.WriteLine($"StatusManager: Updated settings for user {userId}: " +
                        $"IsHardEnabled={isHardEnabled}, PlanetEnabled={planetEnabled}, TierEnabled={tierEnabled}, MissionEnabled={missionEnabled}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StatusManager Fissure: Error updating settings for user {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task<(string IsHardEnabled, string PlanetEnabled, string TierEnabled, string MissionEnabled)> GetFissureSettingsAsync(long userId)
        {
            using var connection = new SQLiteConnection($"Data Source={_dbPathFisssure}");
            await connection.OpenAsync();
            object countResult = await DatabaseHelper.ExecuteScalarAsync(connection, "SELECT COUNT(*) FROM Users WHERE UserId = @UserId", ("@UserId", userId));
            long count = Convert.ToInt64(countResult);
            if (count == 0)
            {
                Console.WriteLine($"StatusManager Fissure: No settings found for user {userId}, returning defaults");
                return (null, null, null, null);
            }

            using var command = new SQLiteCommand("SELECT IsHardEnabled, PlanetEnabled, TierEnabled, MissionEnabled FROM Users WHERE UserId = @UserId", connection);
            command.Parameters.AddWithValue("@UserId", userId);
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    var settings = (
                        IsHardEnabled: reader.IsDBNull(0) ? null : reader.GetString(0),
                        PlanetEnabled: reader.IsDBNull(1) ? null : reader.GetString(1),
                        TierEnabled: reader.IsDBNull(2) ? null : reader.GetString(2),
                        MissionEnabled: reader.IsDBNull(3) ? null : reader.GetString(3)
                    );

                    Console.WriteLine($"StatusManager Fissure: Retrieved settings for user {userId}: {settings}");
                    return settings;
                }
            }
            return (null, null, null, null);
        }




    }
}