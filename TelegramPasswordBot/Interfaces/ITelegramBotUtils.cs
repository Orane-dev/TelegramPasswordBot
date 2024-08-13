using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using TelegramPasswordBot.DTO;

namespace TelegramPasswordBot.Interfaces
{
    public interface ITelegramBotUtils
    {
        Dictionary<string, int> AllowedIds { get; set; }
        string apiEndPoint { get; }
        Task<List<TelegramUserDTO>> GetAllowedUsers();
        void AddToAllowedIds(List<TelegramUserDTO> telegramUsers);
        void AddToAllowedIds(TelegramUserDTO telegramUser);
        Task DeleteMessageASync(ITelegramBotClient botclient, long chatId, int messageId, int delay);
        int GetUserRole(string telegramId);
    }
}
