 **Тг Бот на С#**
### Суть проекта:
В игре Warframe есть множество действий с переодичностью в час, день, неделю.
Но заходить в игру для проверки есть ли какой-нибудь Каскад Бездны Омниа Сп, никому не хочется, суть бота в том,
чтобы отправлять вам уведомления в телеграмме, которые вы выберите.


### Cтруктура проекта 
```
warframetbot
├── Зависимости
├── Bot
│       ├── Menu // меню для выбора уведомлений
│       │   ├── Notification
│       │   │   ├── Fissure
│       │   │   │   ├── IHardMenu.cs
│       │   │   │   ├── MissionMenu.cs
│       │   │   │   ├── PlanetMenu.cs
│       │   │   │   ├── TierMenu.cs
│       │   │   ├── StatusMenu.cs
│       │   │   └── StartMenu.cs
│       ├── Models
│       │   ├── UserState.cs
│       ├── BotEngine.cs // класс для обработки сообщений пользователей
│       └── BotState.cs
├── Db
│   └── Notification
│       └── DbFissure
│        │  ├── DatabaseFissure.cs // база данных выбора пользователей о разломах бездны
│        │  └── DatabaseHelperFissure.cs //лишние забыл удалить
│        ├── DatabaseHelper.cs  // Вывод логов в консоль
│        ├── InitializeArchonHuntDatabase.cs // лишние забыл удалить
│        └── Notification.cs  // База данных выбора пользователей о настройки уведомлений
├── Warframe
│   └── Notifications
│       ├── Class // Классы для чтения апи по определённым запросам
│       │  ├── Archon.cs
│       │  ├── Fissure.cs
│       │  ├── Sortie.cs
│       │  └── Status.cs
│       ├── Reward //Награды для Архонтов и Вылазок (В апи награды не выписываются)
│       │   ├── ArchonReward.cs   
│       │   ├── SortieReward.cs
│       └── Manager.cs  
└── Program.cs // Код для запуска бота и очистки вебхука
``` 
### **Работа бота со стороны пользователя**
___
Команда ```/start``` открывает **StartMenu** (Изначально планировались не только уведомления)
___
<img width="757" height="188" alt="image" src="https://github.com/user-attachments/assets/938398cd-a742-4201-a295-ba75e5e63df6" />

___
При нажатие на кнопку открывается **StatusMenu**
___
<img width="740" height="220" alt="image" src="https://github.com/user-attachments/assets/cc694835-b13f-48bf-8b84-f6a333aa93bf" />

___
В этом меню пользователь включает и выключает уведомления, а также может настроить
При нажатие на кнопку ```⚙️ Настройка``` Открывается меню для настройки уведомлений, где на взгляд разработчика всё интуитивно понятно
___
<img width="433" height="263" alt="image" src="https://github.com/user-attachments/assets/533c0e2b-519b-41bc-961b-0610ee444ea4" />

### **Как работают уведомления**
>Уведомления о статусах отправляются за 15 минут до смены цикла
>
>Уведомления о новой охоте на архонта отправляются в понедельник в 6:00
>
>Уведомления о новой вылазке отправляются ежедневно в 21:00
>
>Уведомления о новых разрывах бездны отправляются в момент открытия разрыва (с погрешностью в минуту)

### **Api Warframe**

Работал с Апи https://api.warframestat.us/pc

Документация к работе с Апи https://docs.warframestat.us/

### **База данных**

Программа использует две базы данныхж
Которые создаются классами ```Notification.cs``` и ```DataBaseFissure.Cs```

**Notification**

_Таблица Users_  
|Name|Value|
|----------------------------------|----------------------------------|
|UserId |INTEGER PRIMARY KEY|
|IsSubscribed| INTEGER NOT NULL DEFAULT 0|
|EarthEnabled| INTEGER NOT NULL DEFAULT 0|
|CetusEnabled| INTEGER NOT NULL DEFAULT 0|
|DeimosEnabled| INTEGER NOT NULL DEFAULT 0|
|VenusEnabled| INTEGER NOT NULL DEFAULT 0|
|LastNotifiedState| TEXT|
|ArchonHuntEnabled| INTEGER DEFAULT 0|
|SortieEnabled|INTEGER DEFAULT 0|
|FissureEnabled |INTEGER DEFAULT 0|

**DataBaseFissure**

_Таблица Fissures_
|Name|Value|
|----------------------------------|----------------------------------|
|Id|INTEGER PRIMARY KEY AUTOINCREMENT|
|Node| TEXT NOT NULL|
|MissionType| TEXT NOT NULL|
|Tier| TEXT NOT NULL|
|Enemy|TEXT NOT NULL|
|Eta| TEXT NOT NULL|
|IsHard| INTEGER NOT NULL|
|TierNum| INTEGER NOT NULL|
|Timestamp| TEXT NOT NULL|

_Таблица Users_
|Name|Value|
|----------------------------------|----------------------------------|
|UserId| INTEGER PRIMARY KEY|
|IsHardEnabled| TEXT NOT NULL|
|PlanetEnabled| TEXT NOT NULL|
|TierEnabled| TEXT NOT NULL|
|MissionEnabled| TEXT NOT NULL|

### Идеи для продолжения проекта:

Создать админ панель, для вывода логов, для создания/обновновления базы данных, для копирования данных из базы данных, просматривания истории отдельного пользователя

Добавление функции Market, для работы с сайтом [Warframe Market](https://warframe.market/ru/ "https://warframe.market/ru/") у сайта также есть своё Api, так что для добавления такой функции нет припятствий, смысл такой функции можете придумать сами, но как идея, находить предметы, которые выгодно продавать. 
>Как пример допустим есть какой нибудь предмет на подобие Мистическая зарядка и её хотят продать за 120 платины, а купить за 100 платины. Вы выставляете лот на покупку за 105 платины, ждёте пока вам кто-то предложит купить, покупаете и выставляете её за 119 платины. И на такой перепродаже зарабатыывать 
