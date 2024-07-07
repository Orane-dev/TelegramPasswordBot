using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramPasswordBot.DTO;

namespace TelegramPasswordBot
{
    internal class PasswordBot
    {
        public List<string> AllowedIds = new List<string> { "1994548862" };

        private readonly ITelegramBotClient _botClient;
        private readonly ReceiverOptions _reciverOptions;

        public PasswordBot(ITelegramBotClient botClient, ReceiverOptions reciverOptions)
        {
            _botClient = botClient;
            _reciverOptions = reciverOptions;
        }

        public async Task Start()
        {
            using var cts = new CancellationTokenSource();
            _botClient.StartReceiving(UpdateHandler, ErrorHandlder, _reciverOptions, cts.Token);
            var botConsole = await _botClient.GetMeAsync();
            Console.WriteLine(botConsole.FirstName);

            await Task.Delay(-1);
        }

        private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        var message = update.Message;
                        var userId = message.From.Id;

                        if (!AllowedIds.Contains(userId.ToString()))
                        {
                            await _botClient.SendTextMessageAsync(message.Chat.Id, "Тебе не разрешено писать мне");
                            return;
                        }
                            
                        else if(message.Text == "/start")
                        {
                            await _botClient.SendTextMessageAsync(message.Chat.Id, "Привет!");
                            return;
                        }
                        else if (message.Text == "/services")
                        {
                            using HttpClient client = new HttpClient();
                            var response = await client.GetAsync($"https://localhost:7198/api/Password/GetUserServices?telegramId={userId}");
                            if (response.IsSuccessStatusCode) {
                                var inlineButtonList = new List<InlineKeyboardButton[]>();
                                var content = await response.Content.ReadFromJsonAsync<Dictionary<int,string>>();
                                foreach (var (k, v) in content)
                                {
                                    inlineButtonList.Add(new InlineKeyboardButton[]
                                    {
                                        InlineKeyboardButton.WithCallbackData(v, v)
                                    });
                                }

                                var inlineKeyboard = new InlineKeyboardMarkup(inlineButtonList);
                                await _botClient.SendTextMessageAsync(message.Chat.Id, "Services", replyMarkup: inlineKeyboard);
                                return;
                            }
                        }
                        else if (message.Text.StartsWith("/create"))
                        {
                            try
                            {
                                var splitedCommand = message.Text.Split(' ');
                                if (splitedCommand.Length == 4)
                                {
                                    string service = splitedCommand[1];
                                    string loginp = splitedCommand[2];
                                    string password = splitedCommand[3];

                                    var postContent = new
                                    {
                                        id = 0,
                                        telegramUserId = userId.ToString(),
                                        passwordServiceName = service,
                                        login = loginp,
                                        encryptedPassword = password,   
                                        createTime = DateTime.UtcNow,
                                    };
                                    var jsonContent = JsonSerializer.Serialize(postContent);
                                    HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                                    using HttpClient postClined = new HttpClient();
                                    var response = await postClined.PostAsync("https://localhost:7198/api/Password/CreatePassword", content);
                                    if (response.IsSuccessStatusCode)
                                    {
                                        await _botClient.SendTextMessageAsync(message.Chat, $"Пароль для сервиса: {service} создан успешно") ;
                                    }
                                }
                            }
                            finally
                            {
                                await _botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                            }
                           
                        }
                        else
                        {
                            _botClient.SendTextMessageAsync(message.Chat, "Такой команды не предусмотрено");
                        }

                        return;
                    case UpdateType.CallbackQuery:
                        var callbackService = update.CallbackQuery.Data;
                        var callBackId = update.CallbackQuery.From.Id;
                        Console.WriteLine(callBackId);
                        using (HttpClient callbackClient = new HttpClient())
                        {
                            var callbackResponse = await callbackClient.GetAsync($"https://localhost:7198/api/Password/GetPassword?telegramId={callBackId.ToString()}&service={callbackService}");
                            if (callbackResponse.IsSuccessStatusCode)
                            {
                                var password = await callbackResponse.Content.ReadFromJsonAsync<PasswordDTO>();
                                var botMessage = await _botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"Login: {password.login}\nPassword: {password.encryptedPassword}");
                                Task.Run(() => DeleteMessage(botMessage.Chat.Id, botMessage.MessageId));
                                return;
                            }
                        }

                        return;
                }
                
            }
            catch (Exception ex)
            {
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        private Task ErrorHandlder(ITelegramBotClient botClient, Exception ex, CancellationToken ct)
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

        private async Task DeleteMessage(long chat, int message)
        {
            await Task.Delay(10000);
            _botClient.DeleteMessageAsync(chat, message);
        }
    }
}
