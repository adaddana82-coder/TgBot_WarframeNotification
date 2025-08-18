using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotProject.Bot;
using warframetgbot.Warframe;

namespace warframetgbot.Bot.Menu.Notifications
{
    public static class PlanetMenu
    {
        public static async Task ShowMenu(ITelegramBotClient bot, long chatId, CancellationToken ct, BotState botState, Dictionary<long, int> lastMessageIds, Manager statusManager)
        {
            ct.ThrowIfCancellationRequested();

            var settings = await statusManager.GetFissureSettingsAsync(chatId);
            var PlanetEnabled = settings.PlanetEnabled;
            Console.WriteLine($"PlanetMenu: Showing menu for user {chatId}: ActivePlanets={PlanetEnabled}");

            if (lastMessageIds.ContainsKey(chatId))
            {
                try
                {
                    await bot.DeleteMessageAsync(chatId, lastMessageIds[chatId], ct);
                    Console.WriteLine($"PlanetMenu: Deleted previous message ID {lastMessageIds[chatId]} for user {chatId}");
                    lastMessageIds.Remove(chatId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PlanetMenu: Failed to delete previous message for user {chatId}: {ex.Message}");
                    // Продолжаем выполнение
                }
            }

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{(PlanetEnabled?.Contains("Earth") == true ? "✅" : "❌")} Земля", "earth_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(PlanetEnabled?.Contains("Mars") == true ? "✅" : "❌")} Марс", "mars_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(PlanetEnabled?.Contains("Venus") == true ? "✅" : "❌")} Венера", "venus_toggle"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{(PlanetEnabled?.Contains("Uranus") == true ? "✅" : "❌")} Уран", "uranus_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(PlanetEnabled?.Contains("Pluto") == true ? "✅" : "❌")} Плутон", "pluto_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(PlanetEnabled?.Contains("Saturn") == true ? "✅" : "❌")} Сатурн", "saturn_toggle"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{(PlanetEnabled?.Contains("Ceres") == true ? "✅" : "❌")} Церера", "ceres_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(PlanetEnabled?.Contains("Eris") == true ? "✅" : "❌")} Эрида", "eris_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(PlanetEnabled?.Contains("Europa") == true ? "✅" : "❌")} Европа", "europa_toggle"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{(PlanetEnabled?.Contains("Sedna") == true ? "✅" : "❌")} Седна", "sedna_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(PlanetEnabled?.Contains("Kuva Fortress") == true ? "✅" : "❌")} Кува Форт", "kuva_fort_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(PlanetEnabled?.Contains("Void") == true ? "✅" : "❌")} Бездна", "void_toggle"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{(PlanetEnabled?.Contains("Jupiter") == true ? "✅" : "❌")} Юпитер", "jupiter_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(PlanetEnabled?.Contains("Lua") == true ? "✅" : "❌")} Луа", "lua_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(PlanetEnabled?.Contains("Zariman") == true ? "✅" : "❌")} Зариман", "zariman_toggle")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{(PlanetEnabled?.Contains("Neptune") == true ? "✅" : "❌")} Нептун", "neptune_toggle"),
                    InlineKeyboardButton.WithCallbackData($"{(PlanetEnabled?.Contains("Phobos") == true ? "✅" : "❌")} Фобос", "phobos_toggle")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("↩️ Назад", "back"),
                    InlineKeyboardButton.WithCallbackData("↪️ Далее", "next")
                }
            });

            var message = await bot.SendTextMessageAsync(
                chatId: chatId,
                text: "Настройка уведомлений по планетам:",
                replyMarkup: keyboard,
                cancellationToken: ct
            );

            lastMessageIds[chatId] = message.MessageId;
            Console.WriteLine($"PlanetMenu: Sent new message ID {message.MessageId} for user {chatId}");
        }

        public static async Task HandleCallbackQuery(ITelegramBotClient bot, long chatId, string callbackData, CallbackQuery callbackQuery, CancellationToken ct, BotState botState, Dictionary<long, int> lastMessageIds, Manager statusManager)
        {
            ct.ThrowIfCancellationRequested();
            Console.WriteLine($"PlanetMenu: User {chatId} selected callback '{callbackData}'");

            var settings = await statusManager.GetFissureSettingsAsync(chatId);
            var PlanetEnabled = string.IsNullOrEmpty(settings.PlanetEnabled)
                ? new string[0]
                : settings.PlanetEnabled.Split(',').Where(p => !string.IsNullOrEmpty(p)).ToArray();
            Console.WriteLine($"PlanetMenu: Current settings for user {chatId} before update: ActivePlanets={string.Join(",", PlanetEnabled)}");

            string newActivePlanets = string.Join(",", PlanetEnabled);

            switch (callbackData)
            {
                case "earth_toggle":
                    TogglePlanet(ref newActivePlanets, "Earth");
                    break;

                case "mars_toggle":
                    TogglePlanet(ref newActivePlanets, "Mars");
                    break;

                case "venus_toggle":
                    TogglePlanet(ref newActivePlanets, "Venus");
                    break;

                case "uranus_toggle":
                    TogglePlanet(ref newActivePlanets, "Uranus");
                    break;

                case "pluto_toggle":
                    TogglePlanet(ref newActivePlanets, "Pluto");
                    break;

                case "saturn_toggle":
                    TogglePlanet(ref newActivePlanets, "Saturn");
                    break;

                case "ceres_toggle":
                    TogglePlanet(ref newActivePlanets, "Ceres");
                    break;

                case "eris_toggle":
                    TogglePlanet(ref newActivePlanets, "Eris");
                    break;

                case "europa_toggle":
                    TogglePlanet(ref newActivePlanets, "Europa");
                    break;

                case "sedna_toggle":
                    TogglePlanet(ref newActivePlanets, "Sedna");
                    break;

                case "kuva_fort_toggle":
                    TogglePlanet(ref newActivePlanets, "Kuva Fortress");
                    break;

                case "void_toggle":
                    TogglePlanet(ref newActivePlanets, "Void");
                    break;

                case "jupiter_toggle":
                    TogglePlanet(ref newActivePlanets, "Jupiter");
                    break;

                case "lua_toggle":
                    TogglePlanet(ref newActivePlanets, "Lua");
                    break;

                case "zariman_toggle":
                    TogglePlanet(ref newActivePlanets, "Zariman");
                    break;

                case "neptune_toggle":
                    TogglePlanet(ref newActivePlanets, "Neptune");
                    break;

                case "phobos_toggle":
                    TogglePlanet(ref newActivePlanets, "Phobos");
                    break;

                case "back":
                    Console.WriteLine($"PlanetMenu: User {chatId} returning to ItHardMenu_Menu");
                    botState.SetUserState(chatId, "ITHARD_MENU");
                    await ItHardMenu.ShowMenu(bot, chatId, ct, botState, lastMessageIds, statusManager);
                    await bot.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: ct);
                    return;

                case "next":
                    Console.WriteLine($"PlanetMenu: User {chatId} returning to Mission_Menu");
                    botState.SetUserState(chatId, "MISSION_MENU");
                    await MissionMenu.ShowMenu(bot, chatId, ct, botState, lastMessageIds, statusManager);
                    await bot.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: ct);
                    return;

                default:
                    Console.WriteLine($"PlanetMenu: User {chatId} sent unknown callback: {callbackData}");
                    break;
            }

            if (!newActivePlanets.Split(',').Where(p => !string.IsNullOrEmpty(p)).SequenceEqual(PlanetEnabled))
            {
                try
                {
                    await statusManager.UpdateFissureSettingsAsync(chatId, settings.IsHardEnabled, newActivePlanets, settings.TierEnabled, settings.MissionEnabled);
                    Console.WriteLine($"PlanetMenu: Successfully updated settings for user {chatId}: ActivePlanets={newActivePlanets}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PlanetMenu: Error updating settings for user {chatId}: {ex.Message}");
                }
            }

            await ShowMenu(bot, chatId, ct, botState, lastMessageIds, statusManager);
            await bot.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: ct);
        }

        private static void TogglePlanet(ref string activePlanets, string planet)
        {
            var planets = string.IsNullOrEmpty(activePlanets)
                ? new List<string>()
                : activePlanets.Split(',').Where(p => !string.IsNullOrEmpty(p)).ToList();

            if (planets.Contains(planet))
            {
                planets.Remove(planet);
            }
            else
            {
                planets.Add(planet);
            }

            activePlanets = string.Join(",", planets);
        }
    }
}