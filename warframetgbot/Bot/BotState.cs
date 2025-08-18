namespace TelegramBotProject.Bot
{
    public class BotState
    {
        private readonly Dictionary<long, string> _userStates = new();

        public void SetUserState(long chatId, string state)
        {
            _userStates[chatId] = state;
        }

        public bool TryGetUserState(long chatId, out string state)
        {
            return _userStates.TryGetValue(chatId, out state!);
        }

        public void RemoveUserState(long chatId)
        {
            _userStates.Remove(chatId);
        }
    }
}