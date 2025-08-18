using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using warframetgbot.Bot.Menu;
using warframetgbot.Bot.Menu.Notifications;
using warframetgbot.Warframe;
using warframetgbot.Warframe.Notifications.Class;
using warframetgbot.Warframe.Notifications.Reward;

namespace TelegramBotProject.Bot
{
    public class BotEngine
    {
        private readonly ITelegramBotClient _botClient;
        private readonly BotState _botState;
        private readonly Manager _statusManager;
        private readonly Dictionary<long, int> _lastMessageIds = new();

        public BotEngine(ITelegramBotClient botClient, Manager statusManager)
        {
            _botClient = botClient;
            _botState = new BotState();
            _statusManager = statusManager;
            Console.WriteLine("BotEngine: Initialized with bot client and StatusManager");
        }

        public void Start()
        {
            Status.Initialize(_botClient, _statusManager);
            _statusManager.Start();
            Console.WriteLine("BotEngine: Started Status, ArchonHunt, Sortie, and StatusManager");
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
            {
                var chatId = update.Message.Chat.Id;
                var messageText = update.Message.Text;

                Console.WriteLine($"BotEngine: Received message from user {chatId}: '{messageText}'");

                EnsureUserInDatabase(chatId);

                if (_botState.TryGetUserState(chatId, out var state))
                {
                    Console.WriteLine($"BotEngine: User {chatId} is in state: {state}");

                    switch (state)
                    {

                        case "STATUS_MENU":
                            await StatusMenu.HandleMenuSelection(botClient, chatId, messageText, cancellationToken, _botState, _lastMessageIds, _statusManager);
                            break;

                        case "SETTINGS_MENU":
                            Console.WriteLine($"BotEngine: Processing SETTINGS_MENU for user {chatId}, but expecting CallbackQuery");
                            break;
                        case "TIER_MENU":
                            Console.WriteLine($"BotEngine: Processing TIER_MENU for user {chatId}, but expecting CallbackQuery");
                            break;
                        case "ITHARD_MENU":
                            Console.WriteLine($"BotEngine: Processing ITHARD_MENU for user {chatId}, but expecting CallbackQuery");
                            break;
                    }

                    
                }

                switch (messageText)
                {
                    case "/start":
                        _botState.SetUserState(chatId, "START_MENU");
                        await StartMenu.ShowMenu(botClient, chatId, cancellationToken, _botState, _lastMessageIds);
                        Console.WriteLine($"BotEngine: User {chatId} started, state set to START_MENU");
                        break;

                 

                    case "Уведомления о Статусах игры":
                        _botState.SetUserState(chatId, "STATUS_MENU");
                        await StatusMenu.ShowMenu(botClient, chatId, cancellationToken, _botState, _lastMessageIds, _statusManager);
                        break;

                  
                }
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                var callbackQuery = update.CallbackQuery;
                var chatId = callbackQuery.Message.Chat.Id;
                var callbackData = callbackQuery.Data;

                Console.WriteLine($"BotEngine: Received CallbackQuery from user {chatId}: '{callbackData}'");

                EnsureUserInDatabase(chatId);

                if (_botState.TryGetUserState(chatId, out var state))
                {
                    switch (state)
                    { 

                        case "SETTINGS_MENU":
                          await SettingsMenu.HandleCallbackQuery(botClient, chatId, callbackData, callbackQuery, cancellationToken, _botState, _lastMessageIds, _statusManager);
                        break;

                        case "TIER_MENU":
                          await TierMenu.HandleCallbackQuery(botClient, chatId, callbackData, callbackQuery, cancellationToken, _botState, _lastMessageIds, _statusManager);
                        break;

                        case "ITHARD_MENU":
                            await ItHardMenu.HandleCallbackQuery(botClient, chatId, callbackData, callbackQuery, cancellationToken, _botState, _lastMessageIds, _statusManager);
                            break;
                        case "PLANET_MENU":
                            await PlanetMenu.HandleCallbackQuery(botClient, chatId, callbackData, callbackQuery, cancellationToken, _botState, _lastMessageIds, _statusManager);
                            break;
                        case "MISSION_MENU":
                            await MissionMenu.HandleCallbackQuery(botClient, chatId, callbackData, callbackQuery, cancellationToken, _botState, _lastMessageIds, _statusManager);
                            break;

                    }
                 
                }
                else
                {
                    Console.WriteLine($"BotEngine: No state for user {chatId}, ignoring CallbackQuery");
                    _botState.SetUserState(chatId, "START_MENU");
                    await StartMenu.ShowMenu(botClient, chatId, cancellationToken, _botState, _lastMessageIds);
                }
            }
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"BotEngine: Error: {exception.Message}\nStackTrace: {exception.StackTrace}");
            return Task.CompletedTask;
        }

        private void EnsureUserInDatabase(long chatId)
        {
            try
            {
                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Db", "warframe_bot.db");
                using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM Users WHERE UserId = @UserId";
                    command.Parameters.AddWithValue("@UserId", chatId);
                    int count = Convert.ToInt32(command.ExecuteScalar());

                    if (count == 0)
                    {
                        command.CommandText = "INSERT INTO Users (UserId, IsSubscribed, EarthEnabled, CetusEnabled, DeimosEnabled, VenusEnabled, LastNotifiedState, ArchonHuntEnabled, SortieEnabled, RelicEnabled) VALUES (@UserId, 0, 0, 0, 0, 0, NULL, 0, 0, 0)";
                        command.Parameters.AddWithValue("@UserId", chatId);
                        command.ExecuteNonQuery();
                    }
                    else
                    {
                        Console.WriteLine($"BotEngine: User {chatId} already exists in database");
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BotEngine: Error ensuring user in database: {ex.Message}");
            }
        }
    }
}