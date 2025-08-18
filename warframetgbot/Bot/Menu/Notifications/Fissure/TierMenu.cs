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
    public static class TierMenu
    {
        public static async Task ShowMenu(ITelegramBotClient bot, long chatId, CancellationToken ct, BotState botState, Dictionary<long, int> lastMessageIds, Manager statusManager)
        {

            var settings = await statusManager.GetFissureSettingsAsync(chatId);
        
            var activeTiers = settings.TierEnabled ?? "";
            Console.WriteLine($"TierMenu: Showing menu for user {chatId}: ActiveTiers={activeTiers}");

            if (lastMessageIds.ContainsKey(chatId))
            {
                try
                {
                    await bot.DeleteMessageAsync(chatId, lastMessageIds[chatId], ct);
                    Console.WriteLine($"TierMenu: Deleted previous message ID {lastMessageIds[chatId]} for user {chatId}");
                    lastMessageIds.Remove(chatId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"TierMenu: Failed to delete previous message for user {chatId}: {ex.Message}");
                    return;
                }
            }

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{(activeTiers.Contains("Lith") ? "✅" : "❌")} Лит", "lit_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(activeTiers.Contains("Meso") ? "✅" : "❌")} Мезо", "mezo_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(activeTiers.Contains("Neo") ? "✅" : "❌")} Нео", "neo_toggle")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{(activeTiers.Contains("Axi") ? "✅" : "❌")} Акси", "axi_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(activeTiers.Contains("Omnia") ? "✅" : "❌")} Омниа", "omnia_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(activeTiers.Contains("Requiem") ? "✅" : "❌")} Кува", "kuva_toggle")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("↩️ Назад", "back"),
                    InlineKeyboardButton.WithCallbackData("↪️ Далее", "next")
                }
            });

            var message = await bot.SendTextMessageAsync(
                chatId: chatId,
                text: "Настройка уведомлений по эрам:",
                replyMarkup: keyboard,
                cancellationToken: ct
            );

            lastMessageIds[chatId] = message.MessageId;
            Console.WriteLine($"TierMenu: Sent new message ID {message.MessageId} for user {chatId}");
        }

        public static async Task HandleCallbackQuery(ITelegramBotClient bot, long chatId, string callbackData, CallbackQuery callbackQuery, CancellationToken ct, BotState botState, Dictionary<long, int> lastMessageIds, Manager statusManager)
        {
            Console.WriteLine($"TierMenu: User {chatId} selected callback '{callbackData}'");
            var settings = await statusManager.GetFissureSettingsAsync(chatId);
            if (settings.GetType == null)
            {
                Console.WriteLine($"TierMenu: Error - settings is null for user {chatId}");
                await bot.SendTextMessageAsync(chatId, "Ошибка: настройки не найдены. Попробуйте позже.", cancellationToken: ct);
                await bot.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: ct);
                return;
            }

            var activeTiers = string.IsNullOrEmpty(settings.TierEnabled)
                ? new string[0]
                : settings.TierEnabled.Split(',').Where(p => !string.IsNullOrEmpty(p)).ToArray();
            Console.WriteLine($"TierMenu: Current settings for user {chatId} before update: ActiveTiers={string.Join(",", activeTiers)}");

            string newActiveTiers = string.Join(",", activeTiers);

            switch (callbackData)
            {
                case "lit_toggle":
                    Toggle(ref newActiveTiers, "Lith");
                    break;
                case "mezo_toggle":
                    Toggle(ref newActiveTiers, "Meso");
                    break;
                case "neo_toggle":
                    Toggle(ref newActiveTiers, "Neo");
                    break;
                case "axi_toggle":
                    Toggle(ref newActiveTiers, "Axi");
                    break;
                case "omnia_toggle":
                    Toggle(ref newActiveTiers, "Omnia");
                    break;
                case "kuva_toggle":
                    Toggle(ref newActiveTiers, "Requiem");
                    break;
                case "back":
                    Console.WriteLine($"TierMenu: User {chatId} returning to Settings_Menu");
                    botState.SetUserState(chatId, "SETTINGS_MENU");
                    await SettingsMenu.ShowMenu(bot, chatId, ct, botState, lastMessageIds, statusManager);
                    await bot.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: ct);
                    return;
                case "next":
                    Console.WriteLine($"TierMenu: User {chatId} returning to ItHard_Menu");
                    botState.SetUserState(chatId, "ITHARD_MENU");
                    await ItHardMenu.ShowMenu(bot, chatId, ct, botState, lastMessageIds, statusManager);
                    await bot.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: ct);
                    return;
                default:
                    Console.WriteLine($"TierMenu: User {chatId} sent unknown callback: {callbackData}");
                    break;
            }

            if (newActiveTiers != string.Join(",", activeTiers))
            {
                try
                {
                    Console.WriteLine($"TierMenu: Updating settings for user {chatId}:");
                    Console.WriteLine($"IsHardEnabled: {settings.IsHardEnabled ?? "NULL"}");
                    Console.WriteLine($"PlanetEnabled: {settings.PlanetEnabled ?? "NULL"}");
                    Console.WriteLine($"TierEnabled: {newActiveTiers}");
                    Console.WriteLine($"MissionEnabled: {settings.MissionEnabled ?? "NULL"}");

                    await statusManager.UpdateFissureSettingsAsync(
                        chatId,
                        settings.IsHardEnabled ?? "",
                        settings.PlanetEnabled ?? "",
                        newActiveTiers,
                        settings.MissionEnabled ?? ""
                    );
                    Console.WriteLine($"TierMenu: Successfully updated settings for user {chatId}: ActiveTiers={newActiveTiers}");
                }
                catch (SQLiteException ex)
                {
                    Console.WriteLine($"TierMenu: Database error for user {chatId}: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                    await bot.SendTextMessageAsync(chatId, "Ошибка базы данных. Обратитесь к администратору.", cancellationToken: ct);
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"TierMenu: Error updating settings for user {chatId}: {ex.Message}");
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