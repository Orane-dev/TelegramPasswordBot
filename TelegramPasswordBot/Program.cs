using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using TelegramPasswordBot.Interfaces;
using TelegramPasswordBot.Handlers;
using TelegramPasswordBot.Utils;

namespace TelegramPasswordBot
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Environment.SetEnvironmentVariable("BOT_TOKEN", "7119097284:AAFnWy8rK5a31jncZOhQCw8i69voJpAObqo");
            Environment.SetEnvironmentVariable("DEBUG", "localhost:7198");
            Environment.SetEnvironmentVariable("RELEASE", "passwordapimanager:443");
            var services = new ServiceCollection();
            ConfigureServices(services);

            var serviceProvider = services.BuildServiceProvider();

            var botService = serviceProvider.GetRequiredService<IPasswordBot>();
            await botService.Start();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddSingleton<IPasswordBot, PasswordBot>();
            services.AddTransient<BotMessageHandlers>();
            services.AddTransient<BotCallbackQueryHandlers>();
            services.AddSingleton<ITelegramBotUtils, TelegramBotUtils>();
            services.AddSingleton<ITelegramBotClient>(provider => new TelegramBotClient(Environment.GetEnvironmentVariable("BOT_TOKEN")));
        }
    }
}
