namespace TelegramBotProject.Bot.Models
{
    public class UserState
    {
        public string CurrentState { get; set; }
        public Dictionary<string, object> TempData { get; } = new();
    }
}