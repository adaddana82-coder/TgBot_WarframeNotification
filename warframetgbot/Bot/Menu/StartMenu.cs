using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotProject.Bot;

namespace warframetgbot.Bot.Menu
{
    public static class StartMenu
    {
        public static async Task ShowMenu(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken, BotState botState, Dictionary<long, int> lastMessageIds)
        {
            Console.WriteLine($"StartMenu: Showing menu for user {chatId}");
        

            var replyMarkup = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton("Уведомления о Статусах игры")
            })
            {
                ResizeKeyboard = true
            };

            var message = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Выберите действие:",
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken
            );

            // Сохраняем ID нового сообщения
            lastMessageIds[chatId] = message.MessageId;
            Console.WriteLine($"StartMenu: Sent new message ID {message.MessageId} for user {chatId}");
        }


        public static async Task HandleMenuSelection(ITelegramBotClient bot, long chatId, string text, CancellationToken ct, BotState botState, Dictionary<long, int> lastMessageIds)
        {
            Console.WriteLine($"StartMenu: User {chatId} selected '{text}'");

            switch (text)
            {
                default:
                    Console.WriteLine($"StartMenu: User {chatId} sent unknown command: {text}");
                    await ShowMenu(bot, chatId, ct, botState, lastMessageIds);
                    break;
            }
        }
    }
}