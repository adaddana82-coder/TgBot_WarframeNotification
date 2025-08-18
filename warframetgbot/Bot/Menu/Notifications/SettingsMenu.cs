using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotProject.Bot;
using warframetgbot.Warframe;

namespace warframetgbot.Bot.Menu.Notifications
{
    public static class SettingsMenu
    {
        public static async Task ShowMenu(ITelegramBotClient bot, long chatId, CancellationToken ct, BotState botState, Dictionary<long, int> lastMessageIds, Manager statusManager)
        {
            var (IsSubscribed, EarthEnabled, CetusEnabled, DeimosEnabled, VenusEnabled, _, ArchonHuntEnabled, SortieEnabled, FissureEnabled) = await statusManager.GetUserSettingsAsync(chatId);
            Console.WriteLine($"SettingsMenu: Showing menu for user {chatId}: Earth={EarthEnabled}, Cetus={CetusEnabled}, Deimos={DeimosEnabled}, Venus={VenusEnabled}, ArchonHunt={ArchonHuntEnabled}, Sortie = {SortieEnabled}, RelicEnabled = {FissureEnabled}");

            if (lastMessageIds.ContainsKey(chatId))
            {
                try
                {
                    if (botState.TryGetUserState(chatId, out var state))
                    {
                        switch (state)
                        {
                            case "SETTINGS_MENU":
                                await bot.DeleteMessageAsync(chatId, lastMessageIds[chatId], ct);
                                Console.WriteLine($"SettingsMenu: Deleted previous message ID {lastMessageIds[chatId]} for user {chatId}");
                                lastMessageIds.Remove(chatId);
                                break;

                            default:
                                Console.WriteLine($"SettingsMenu: No Deleted previous message ID {lastMessageIds[chatId]} for user {chatId}");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SettingsMenu: Failed to delete previous message for user {chatId}: {ex.Message}");
                    return;
                }
            }

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{(EarthEnabled ? "✅" : "❌")} Смена статуса на Земле", "earth_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(CetusEnabled ? "✅" : "❌")} Смена статуса на Цетусе", "cetus_toggle")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{(DeimosEnabled ? "✅" : "❌")} Смена статуса на Деймосе", "deimos_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(VenusEnabled ? "✅" : "❌")} Смена статуса на Венере", "venus_toggle")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{(ArchonHuntEnabled ? "✅" : "❌")} Уведомлять о новом архонте", "archon_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(SortieEnabled ? "✅" : "❌")} Уведомлять о новой вылазки", "sortie_toggle")

                },
                    new[]
                {
                    InlineKeyboardButton.WithCallbackData($"Уведомления о реликвиях", "relic_toggle")
                //   , InlineKeyboardButton.WithCallbackData($"{(RelicEnabled ? "✅" : "❌")} Уведомлять о реликвиях  CП", "relicIsHard_toggle")


                },
                new[] { InlineKeyboardButton.WithCallbackData("↩️ Назад", "back") }
            });

            var message = await bot.SendTextMessageAsync(
                chatId: chatId,
                text: "Настройка уведомлений:",
                replyMarkup: keyboard,
                cancellationToken: ct
            );

            lastMessageIds[chatId] = message.MessageId;
            Console.WriteLine($"SettingsMenu: Sent new message ID {message.MessageId} for user {chatId}");
        }

        public static async Task HandleCallbackQuery(ITelegramBotClient bot, long chatId, string callbackData, CallbackQuery callbackQuery, CancellationToken ct, BotState botState, Dictionary<long, int> lastMessageIds, Manager statusManager)
        {
            Console.WriteLine($"SettingsMenu: User {chatId} selected callback '{callbackData}'");

            var (IsSubscribed, EarthEnabled, CetusEnabled, DeimosEnabled, VenusEnabled, LastNotifiedState, ArchonHuntEnabled, SortieEnabled, FissureEnabled) = await statusManager.GetUserSettingsAsync(chatId);
            Console.WriteLine($"SettingsMenu: Current settings for user {chatId} before update: ArchonHuntEnabled={ArchonHuntEnabled}, SortieEnabled = {SortieEnabled}, RelicEnabled = {FissureEnabled}");

            switch (callbackData)
            {
                case "earth_toggle":
                    EarthEnabled = !EarthEnabled;
                    Console.WriteLine($"SettingsMenu: Toggling EarthEnabled for user {chatId} to {EarthEnabled}");
                    break;

                case "cetus_toggle":
                    CetusEnabled = !CetusEnabled;
                    Console.WriteLine($"SettingsMenu: Toggling CetusEnabled for user {chatId} to {CetusEnabled}");
                    break;

                case "deimos_toggle":
                    DeimosEnabled = !DeimosEnabled;
                    Console.WriteLine($"SettingsMenu: Toggling DeimosEnabled for user {chatId} to {DeimosEnabled}");
                    break;

                case "venus_toggle":
                    VenusEnabled = !VenusEnabled;
                    Console.WriteLine($"SettingsMenu: Toggling VenusEnabled for user {chatId} to {VenusEnabled}"); 
                    break;
                 
                case "archon_toggle":
                    ArchonHuntEnabled = !ArchonHuntEnabled;
                    Console.WriteLine($"SettingsMenu: Toggling ArchonHuntEnabled for user {chatId} to {ArchonHuntEnabled}");
                    break;

                case "sortie_toggle":
                    SortieEnabled = !SortieEnabled;
                    Console.WriteLine($"SettingsMenu: Toggling SortieEnabled for user {chatId} to {SortieEnabled}");
                    break;

                case "relic_toggle":
                    Console.WriteLine($"SettingsMenu: User {chatId} returning to FISSURE_MENU");
                    botState.SetUserState(chatId, "TIER_MENU");
                    await TierMenu.ShowMenu(bot, chatId, ct, botState, lastMessageIds, statusManager);
                    await bot.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: ct); Console.WriteLine($"SettingsMenu: Toggling RelicEnabled for user {chatId} to {FissureEnabled}");
                    break; 

                case "back":
                    Console.WriteLine($"SettingsMenu: User {chatId} returning to STATUS_MENU");
                    botState.SetUserState(chatId, "STATUS_MENU");
                    await StatusMenu.ShowMenu(bot, chatId, ct, botState, lastMessageIds, statusManager);
                    await bot.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: ct);
                    return;

                default:
                    Console.WriteLine($"SettingsMenu: User {chatId} sent unknown callback: {callbackData}");
                    break;

            }

            try
            {
                await statusManager.UpdateUserSettingsAsync(chatId, IsSubscribed, EarthEnabled, CetusEnabled, DeimosEnabled, VenusEnabled, LastNotifiedState, ArchonHuntEnabled, SortieEnabled, FissureEnabled);
                Console.WriteLine($"SettingsMenu: Successfully updated settings for user {chatId}: ArchonHuntEnabled={ArchonHuntEnabled}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SettingsMenu: Error updating settings for user {chatId}: {ex.Message}");
            }
            if (botState.TryGetUserState(chatId, out var state))
            {
                if (state == "SETTINGS_MENU")
                {
                    await ShowMenu(bot, chatId, ct, botState, lastMessageIds, statusManager);
                    await bot.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: ct);
                }

            }
               
        }
    }
}