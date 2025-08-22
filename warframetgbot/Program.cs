using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using warframetgbot.Warframe;
using TelegramBotProject.Bot;
namespace warframetgbot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddSingleton<Manager>();
            services.AddSingleton<ITelegramBotClient>(provider => new TelegramBotClient("YOUR_TOKEN"));
            services.AddSingleton<BotEngine>();
            var serviceProvider = services.BuildServiceProvider();

            var botEngine = serviceProvider.GetService<BotEngine>();

            var botClient = serviceProvider.GetService<ITelegramBotClient>();
            await botClient.DeleteWebhookAsync();
            botEngine.Start();
            await botClient.ReceiveAsync(botEngine.HandleUpdateAsync, botEngine.HandleErrorAsync);
        }
    }
}
