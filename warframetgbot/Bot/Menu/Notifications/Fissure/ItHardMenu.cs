using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotProject.Bot;
using warframetgbot.Warframe;

namespace warframetgbot.Bot.Menu.Notifications
{
    public static class ItHardMenu
    {
        public static async Task ShowMenu(ITelegramBotClient bot, long chatId, CancellationToken ct, BotState botState, Dictionary<long, int> lastMessageIds, Manager statusManager)
        {

            var settings = await statusManager.GetFissureSettingsAsync(chatId);
            var ItHardTrueEnabled = settings.IsHardEnabled ?? "";

            Console.WriteLine($"IthHardMenu: Showing menu for user {chatId}: IthHardTrue={ItHardTrueEnabled}");

            if (lastMessageIds.ContainsKey(chatId))
            {
                try
                {
                    await bot.DeleteMessageAsync(chatId, lastMessageIds[chatId], ct);
                    Console.WriteLine($"IthHardMenu: Deleted previous message ID {lastMessageIds[chatId]} for user {chatId}");
                    lastMessageIds.Remove(chatId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"IthHardMenu: Failed to delete previous message for user {chatId}: {ex.Message}");
                    return;
                }
            }

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                { 
                       InlineKeyboardButton.WithCallbackData($"{(ItHardTrueEnabled.Contains("true") ? "✅" : "❌")} Стальной путь", "true_toggle"),
                       InlineKeyboardButton.WithCallbackData($"{(ItHardTrueEnabled.Contains("false") ? "✅" : "❌")} Обычный", "false_toggle")
                },
                 new[]
                {
                    InlineKeyboardButton.WithCallbackData("↩️ Назад", "back"),
                    InlineKeyboardButton.WithCallbackData("↪️ Далее", "next")
                }
            });

            var message = await bot.SendTextMessageAsync(
                chatId: chatId,
                text: "Настройка уведомлений по сложности:",
                replyMarkup: keyboard,
                cancellationToken: ct
            );

            lastMessageIds[chatId] = message.MessageId;
            Console.WriteLine($"IthHardMenu: Sent new message ID {message.MessageId} for user {chatId}");
        }

        public static async Task HandleCallbackQuery(ITelegramBotClient bot, long chatId, string callbackData, CallbackQuery callbackQuery, CancellationToken ct, BotState botState, Dictionary<long, int> lastMessageIds, Manager statusManager)
        {
            Console.WriteLine($"IthHardMenu: User {chatId} selected callback '{callbackData}'");

            var settings = await statusManager.GetFissureSettingsAsync(chatId);
            var ItHardTrueEnabled = settings.IsHardEnabled;
            Console.WriteLine($"IthHardMenu: Current settings for user {chatId} before update: IthHardTrue={ItHardTrueEnabled}");
            string newItHardTrueEnabled = string.Join(",", ItHardTrueEnabled);

            switch (callbackData)
            {
                case "true_toggle":
                    Toggle(ref newItHardTrueEnabled, "true");
                    break;
                case "false_toggle":
                    Toggle(ref newItHardTrueEnabled, "false");
                    break;

                case "back":
                    Console.WriteLine($"IthHardMenu: User {chatId} returning to Tier_Menu");
                    botState.SetUserState(chatId, "TIER_MENU");
                    await TierMenu.ShowMenu(bot, chatId, ct, botState, lastMessageIds, statusManager);
                    await bot.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: ct);
                    return;
                case "next":
                    Console.WriteLine($"IthHardMenu: User {chatId} returning to PLANET_MENU");
                    botState.SetUserState(chatId, "PLANET_MENU");
                    await PlanetMenu.ShowMenu(bot, chatId, ct, botState, lastMessageIds, statusManager);
                    await bot.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: ct);
                    return;

                default:
                    Console.WriteLine($"IthHardMenu: User {chatId} sent unknown callback: {callbackData}");
                    break;
            }

            if (newItHardTrueEnabled != string.Join(",", ItHardTrueEnabled))
            {
                try
                {
                    Console.WriteLine($"IsHardEnabled: Updating settings for user {chatId}:");
                    Console.WriteLine($"IsHardEnabled: {newItHardTrueEnabled}");
                    
                    await statusManager.UpdateFissureSettingsAsync(
                        chatId,
                        newItHardTrueEnabled,
                        settings.PlanetEnabled ?? "",
                        settings.TierEnabled ?? "",
                        settings.MissionEnabled ?? ""
                    );
                    Console.WriteLine($"IsHardEnabled: Successfully updated settings for user {chatId}: ItHardEnabled={newItHardTrueEnabled}");
                }
                catch (SQLiteException ex)
                {
                    Console.WriteLine($"IsHardEnabled: Database error for user {chatId}: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                    await bot.SendTextMessageAsync(chatId, "Ошибка базы данных. Обратитесь к администратору.", cancellationToken: ct);
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"IsHardEnabled: Error updating settings for user {chatId}: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                    await bot.SendTextMessageAsync(chatId, "Ошибка при сохранении настроек. Попробуйте позже.", cancellationToken: ct);
                    return;
                }
            }

            await ShowMenu(bot, chatId, ct, botState, lastMessageIds, statusManager);
            await bot.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: ct);
        }
        private static void Toggle(ref string activeFun, string Fun)
        {
            var tiers = activeFun.Split(',').Where(t => !string.IsNullOrEmpty(t)).ToList();
            if (tiers.Contains(Fun))
            {
                tiers.Remove(Fun); // Выключаем, если уже включена
            }
            else
            {
                tiers.Add(Fun); // Включаем, если выключена
            }
            activeFun = string.Join(",", tiers);
        }
    }
}