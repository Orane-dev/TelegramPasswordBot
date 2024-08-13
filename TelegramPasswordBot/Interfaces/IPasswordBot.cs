using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace TelegramPasswordBot.Interfaces
{
    public interface IPasswordBot
    {
        Dictionary<string, int> AllowedIds { get; set; }
        Task Start();
        Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken ct);
        Task ErrorHandlder(ITelegramBotClient botClient, Exception ex, CancellationToken ct);
    }
}
