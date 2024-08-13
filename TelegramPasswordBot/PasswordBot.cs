using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramPasswordBot.DTO;
using TelegramPasswordBot.Handlers;
using TelegramPasswordBot.Interfaces;
using TelegramPasswordBot.Utils;

namespace TelegramPasswordBot
{
    public class PasswordBot : IPasswordBot
    {
        public Dictionary<string, int> AllowedIds { get; set; } = new Dictionary<string, int>();
        private readonly ITelegramBotClient _botClient;
        private readonly ITelegramBotUtils _telegramBotUtils;
        private readonly ReceiverOptions _reciverOptions;
        private readonly BotMessageHandlers _messageHandlers;
        private readonly BotCallbackQueryHandlers _callbackHandlers;
        private IServiceProvider _serviceProvider;

        public PasswordBot(ITelegramBotClient botClient, BotMessageHandlers messageHandlers, BotCallbackQueryHandlers callbackHandlers, ITelegramBotUtils botUtils)
        {
            _botClient = botClient;
            _telegramBotUtils = botUtils;
            _reciverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[]
                {
                    UpdateType.Message,
                    UpdateType.InlineQuery,
                    UpdateType.CallbackQuery,
                },
                ThrowPendingUpdates = true
            };
            _messageHandlers = messageHandlers;
            _callbackHandlers = callbackHandlers;

        }

        public async Task Start()
        {
            var allowedUser = await _telegramBotUtils.GetAllowedUsers();
            if(allowedUser.Any())
            {
                _telegramBotUtils.AddToAllowedIds(allowedUser);
            }

            using var cts = new CancellationTokenSource();
            _botClient.StartReceiving(UpdateHandler, ErrorHandlder, _reciverOptions, cts.Token);
            
            var botConsole = await _botClient.GetMeAsync();
            Console.WriteLine(botConsole.FirstName);

            await Task.Delay(-1);
        }

        public async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        var message = update.Message;
                        var userId = message.From.Id;

                        if (!_telegramBotUtils.AllowedIds.ContainsKey(userId.ToString()))
                        {
                            await _botClient.SendTextMessageAsync(message.Chat.Id, "Тебе не разрешено писать мне");
                            return;
                        }

                        else if (message.Text == "/start")
                        {
                            await _botClient.SendTextMessageAsync(message.Chat.Id, "Привет!");
                            return;
                        }
                        else if (message.Text == "/services")
                        {
                            await _messageHandlers.GetUserServicesHandlerAsync(update);
                        }
                        else if (message.Text.StartsWith("/create"))
                        {
                            await _messageHandlers.CreatePasswordHandlerAsync(update);
                        }
                        else if (message.Text.StartsWith("/delete"))
                        {
                            await _messageHandlers.DeletePasswordHandlerAsync(update);
                        }
                        else if (message.Text.StartsWith("/update"))
                        {
                            await _messageHandlers.PutPasswordHandlerAsync(update);
                        }
                        else if (message.Text.StartsWith("/register"))
                        {
                            await _messageHandlers.RegisterUserHandlerAsync(update);
                        }
                        else
                        {
                            _botClient.SendTextMessageAsync(message.Chat, "Такой команды не предусмотрено");
                        }
                        break;
                    case UpdateType.CallbackQuery:
                        var callbackData = update.CallbackQuery.Data;

                        if (callbackData.StartsWith("service_"))
                        {
                            await _callbackHandlers.GetPasswordCallbackAsync(update);
                        }
                        else if (callbackData.StartsWith("login_"))
                        {
                            await _callbackHandlers.GetPasswordByLoginAsync(update);
                        }
                        else {
                            break;
                        }
                        break;
                }
                
            }
            catch (Exception ex)
            {
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public Task ErrorHandlder(ITelegramBotClient botClient, Exception ex, CancellationToken ct)
        {
            var ErrorMessage = ex switch
            {
                ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => ex.ToString()
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}
