using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotProject.Bot;
using warframetgbot.Warframe;

namespace warframetgbot.Bot.Menu.Notifications
{
    public static class MissionMenu
    {
        public static async Task ShowMenu(ITelegramBotClient bot, long chatId, CancellationToken ct, BotState botState, Dictionary<long, int> lastMessageIds, Manager statusManager)
        {
            var settings = await statusManager.GetFissureSettingsAsync(chatId);
            var activeMissions = settings.MissionEnabled;
            Console.WriteLine($"MissionMenu: Showing menu for user {chatId}: ActiveMissions={activeMissions}");

            if (lastMessageIds.ContainsKey(chatId))
            {
                try
                {
                    await bot.DeleteMessageAsync(chatId, lastMessageIds[chatId], ct);
                    Console.WriteLine($"MissionMenu: Deleted previous message ID {lastMessageIds[chatId]} for user {chatId}");
                    lastMessageIds.Remove(chatId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"MissionMenu: Failed to delete previous message for user {chatId}: {ex.Message}");
                    return;
                }
            }

            var keyboard = new InlineKeyboardMarkup(new[]
            {
 
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{(activeMissions.Contains("Extermination, Assault") ? "✅" : "❌")} Зачистка", "assault_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(activeMissions.Contains("Capture") ? "✅" : "❌")} Захват", "capture_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(activeMissions.Contains("Defense") ? "✅" : "❌")} Оборона", "defense_toggle"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{(activeMissions.Contains("Disruption") ? "✅" : "❌")} Сбой", "disruption_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(activeMissions.Contains("Excavation") ? "✅" : "❌")} Раскопки", "excavation_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(activeMissions.Contains("Void Flood") ? "✅" : "❌")} Потоп Бездны", "void_flood_toggle"),

                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{(activeMissions.Contains("Interception") ? "✅" : "❌")} Перехват", "intercep_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(activeMissions.Contains("Mobile Defense") ? "✅" : "❌")} Моб. Оборона", "mobile_def_toggle"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{(activeMissions.Contains("Rescue") ? "✅" : "❌")} Спасение", "rescue_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(activeMissions.Contains("Sabotage") ? "✅" : "❌")} Диверсия", "sabotage_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(activeMissions.Contains("Spy") ? "✅" : "❌")} Шпионаж", "spy_toggle"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{(activeMissions.Contains("Survival") ? "✅" : "❌")} Выживание", "survival_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(activeMissions.Contains("Void Casc") ? "✅" : "❌")} Каскад Бездны", "void_casc_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(activeMissions.Contains("Alchemy") ? "✅" : "❌")} Алхимия", "alchemy_toggle"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("↩️ Назад", "back"),
                    InlineKeyboardButton.WithCallbackData("↪️ Далее", "next")
                }
            });

            var message = await bot.SendTextMessageAsync(
                chatId: chatId,
                text: "Настройка уведомлений по миссиям:",
                replyMarkup: keyboard,
                cancellationToken: ct
            );

            lastMessageIds[chatId] = message.MessageId;
            Console.WriteLine($"MissionMenu: Sent new message ID {message.MessageId} for user {chatId}");
        }

        public static async Task HandleCallbackQuery(ITelegramBotClient bot, long chatId, string callbackData, CallbackQuery callbackQuery, CancellationToken ct, BotState botState, Dictionary<long, int> lastMessageIds, Manager statusManager)
        {
            Console.WriteLine($"MissionMenu: User {chatId} selected callback '{callbackData}'");

            var settings = await statusManager.GetFissureSettingsAsync(chatId);
            var activeMissions = string.IsNullOrEmpty(settings.MissionEnabled)
                ? new string[0]
                : settings.MissionEnabled.Split(',').Where(p => !string.IsNullOrEmpty(p)).ToArray(); Console.WriteLine($"MissionMenu: Current settings for user {chatId} before update: ActiveMissions={string.Join(",", activeMissions)}");

            string newActiveMissions = string.Join(",", activeMissions); // Начальное значение

            switch (callbackData)
            {
              
                case "assault_toggle":
                    ToggleMission(ref newActiveMissions, "Extermination, Assault");
                    break;

                case "capture_toggle":
                    ToggleMission(ref newActiveMissions, "Capture");
                    break;

                case "defense_toggle":
                    ToggleMission(ref newActiveMissions, "Defense");
                    break;

                case "disruption_toggle":
                    ToggleMission(ref newActiveMissions, "Disruption");
                    break;

                case "excavation_toggle":
                    ToggleMission(ref newActiveMissions, "Excavation");
                    break;

                case "extermin_toggle":
                    ToggleMission(ref newActiveMissions, "Extermin");
                    break;

                case "hive_toggle":
                    ToggleMission(ref newActiveMissions, "Hive");
                    break;

                case "intercep_toggle":
                    ToggleMission(ref newActiveMissions, "Interception");
                    break;

                case "mobile_def_toggle":
                    ToggleMission(ref newActiveMissions, "Mobile Defense");
                    break;

                case "rescue_toggle":
                    ToggleMission(ref newActiveMissions, "Rescue");
                    break;

                case "sabotage_toggle":
                    ToggleMission(ref newActiveMissions, "Sabotage");
                    break;

                case "spy_toggle":
                    ToggleMission(ref newActiveMissions, "Spy");
                    break;

                case "survival_toggle":
                    ToggleMission(ref newActiveMissions, "Survival");
                    break;

                case "void_casc_toggle":
                    ToggleMission(ref newActiveMissions, "Void Casc");
                    break;

                case "alchemy_toggle":
                    ToggleMission(ref newActiveMissions, "Alchemy");
                    break;

                case "void_flood_toggle":
                    ToggleMission(ref newActiveMissions, "Void Flood");
                    break;

                case "back":
                    Console.WriteLine($"MissionMenu: User {chatId} returning to Planet_Menu");
                    botState.SetUserState(chatId, "PLANET_MENU");
                    await PlanetMenu.ShowMenu(bot, chatId, ct, botState, lastMessageIds, statusManager);
                    await bot.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: ct);
                    return;

                case "next":
                    Console.WriteLine($"MissionMenu: User {chatId} returning to Settings_Menu");
                    botState.SetUserState(chatId, "SETTINGS_MENU");
                    await SettingsMenu.ShowMenu(bot, chatId, ct, botState, lastMessageIds, statusManager);
                    await bot.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: ct);
                    return;

                default:
                    Console.WriteLine($"MissionMenu: User {chatId} sent unknown callback: {callbackData}");
                    break;
            }

            if (newActiveMissions != string.Join(",", activeMissions))
            {
                try
                {
                    await statusManager.UpdateFissureSettingsAsync(chatId, settings.IsHardEnabled, settings.PlanetEnabled, settings.TierEnabled, newActiveMissions);
                    Console.WriteLine($"MissionMenu: Successfully updated settings for user {chatId}: ActiveMissions={newActiveMissions}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"MissionMenu: Error updating settings for user {chatId}: {ex.Message}");
                }
            }

            await ShowMenu(bot, chatId, ct, botState, lastMessageIds, statusManager);
            await bot.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: ct);
        }

        private static void ToggleMission(ref string activeFun, string Fun)
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