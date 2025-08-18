
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotProject.Bot;
using warframetgbot.Warframe;

namespace warframetgbot.Bot.Menu.Notifications
{
    public static class StatusMenu
    {
        public static async Task ShowMenu(ITelegramBotClient bot, long chatId, CancellationToken ct, BotState botState, Dictionary<long, int> lastMessageIds, Manager statusManager)
        {
            Console.WriteLine($"StatusMenu: Showing menu for user {chatId}");

            if (lastMessageIds.ContainsKey(chatId))
            {
                try
                {
                    await bot.DeleteMessageAsync(chatId, lastMessageIds[chatId], ct);
                    Console.WriteLine($"StatusMenu: Deleted previous message ID {lastMessageIds[chatId]} for user {chatId}");
                    lastMessageIds.Remove(chatId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"StatusMenu: Failed to delete previous message for user {chatId}: {ex.Message}");
                    return;
                }
            }

            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("🔔 Вкл"), new KeyboardButton("⚙ Настройка"), new KeyboardButton("🔕 Выкл") },
                new[] { new KeyboardButton("↩️ Назад") }
            })
            {
                ResizeKeyboard = true
            };

            var message = await bot.SendTextMessageAsync(
                chatId: chatId,
                text: "Управление уведомлениями:",
                replyMarkup: keyboard,
                cancellationToken: ct
            );

            lastMessageIds[chatId] = message.MessageId;
            Console.WriteLine($"StatusMenu: Sent new message ID {message.MessageId} for user {chatId}");
        }

        public static async Task HandleMenuSelection(ITelegramBotClient bot, long chatId, string text, CancellationToken ct, BotState botState, Dictionary<long, int> lastMessageIds, Manager statusManager)
        {
            Console.WriteLine($"StatusMenu: User {chatId} selected '{text}'");
            var (IsSubscribed, EarthEnabled, CetusEnabled, DeimosEnabled, VenusEnabled, LastNotifiedState, ArchonHuntEnabled, SortieEnabled, RelicEnabled) = await statusManager.GetUserSettingsAsync(chatId);

            switch (text)
            {
                case "🔔 Вкл":
                    IsSubscribed = true;
                    await statusManager.UpdateUserSettingsAsync(chatId, IsSubscribed, EarthEnabled, CetusEnabled, DeimosEnabled, VenusEnabled, LastNotifiedState, ArchonHuntEnabled, SortieEnabled, RelicEnabled);
                    await bot.SendTextMessageAsync(chatId, "Уведомления включены!", cancellationToken: ct);
                    break;
                case "🔕 Выкл":
                    IsSubscribed = false;
                    await statusManager.UpdateUserSettingsAsync(chatId, IsSubscribed, EarthEnabled, CetusEnabled, DeimosEnabled, VenusEnabled, LastNotifiedState, ArchonHuntEnabled, SortieEnabled, RelicEnabled);
                    await bot.SendTextMessageAsync(chatId, "Уведомления выключены!", cancellationToken: ct);
                    break;
                case "⚙ Настройка":
                case "? Настройка":
                    Console.WriteLine($"StatusMenu: User {chatId} transitioning to SETTINGS_MENU");
                    botState.SetUserState(chatId, "SETTINGS_MENU");
                    await SettingsMenu.ShowMenu(bot, chatId, ct, botState, lastMessageIds, statusManager);
                    break;
                case "↩️ Назад":
                    Console.WriteLine($"StatusMenu: User {chatId} returning to START_MENU");
                    botState.SetUserState(chatId, "START_MENU");
                    await StartMenu.ShowMenu(bot, chatId, ct, botState, lastMessageIds);
                    break;
            }
        }
    }
}