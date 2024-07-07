using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace TelegramPasswordBot
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            PasswordBot bot = new PasswordBot(new TelegramBotClient("7126133309:AAFChXgWwFJ7tdTeGqkNASlkXgbWblJlMFw"), new ReceiverOptions
            {
                AllowedUpdates = new[]
                {
                    UpdateType.Message,
                    UpdateType.InlineQuery,
                    UpdateType.CallbackQuery,
                },
                ThrowPendingUpdates = true
            });

            await bot.Start();
        }
    }
}
