using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace warframetgbot.Warframe.Notifications.Reward
{
    public  class SortieReward
    {
            public static string[] CommonRewards = new[]
            {
                        "Эндо (4000)",
                        "Скульптура Анаса",
                        "Кува (6000)"
            };

            public static string[] UncommonRewards = new[]
            {
                        "Ривен-мод (Винтовка, Пистолет, Дробовик, Ближний бой)",
                        "Чертеж Формы",
                        "Усилитель на 3 дня (Синтез, Ресурсы, Моды)"
             };

            public static string[] RareRewards = new[]
            {
                        "Чертеж Адаптера Экзилус",
                        "Чертеж Орокин Катализатора",
                        "Чертеж Орокин Реактора",
                        "Легендарное ядро"
            };


    }
}

