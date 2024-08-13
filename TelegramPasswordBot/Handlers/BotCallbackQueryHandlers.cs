using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramPasswordBot.DTO;
using TelegramPasswordBot.Interfaces;
using TelegramPasswordBot.Utils;

namespace TelegramPasswordBot.Handlers
{
    public class BotCallbackQueryHandlers
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ITelegramBotUtils _botUtils;
        private Dictionary<long, List<PasswordDTO>> _passwordCache = new Dictionary<long, List<PasswordDTO>>();
        public BotCallbackQueryHandlers(ITelegramBotClient botClient, IHttpClientFactory clientFactory, ITelegramBotUtils botUtils) 
        {
            _botClient = botClient;
            _clientFactory = clientFactory;
            _botUtils = botUtils;
        }

        public async Task GetPasswordCallbackAsync(Update update)
        {
            var callbackService = update.CallbackQuery.Data;
            var callBackId = update.CallbackQuery.From.Id;

            using (HttpClient callbackClient = _clientFactory.CreateClient())
            {
                try
                {
                    var callbackResponse = await callbackClient.GetAsync(
                   $"https://{_botUtils.apiEndPoint}/api/Password?telegramId={callBackId.ToString()}&" +
                   $"service={callbackService.Substring("service_".Length)}");

                    if (callbackResponse.IsSuccessStatusCode)
                    {
                        var passwordList = await callbackResponse.Content.ReadFromJsonAsync<List<PasswordDTO>>();
                        if (passwordList.Count > 1)
                        {
                            _passwordCache[callBackId] = passwordList;
                            var inlineButtonList = new List<InlineKeyboardButton[]>();
                            foreach (var password in passwordList)
                            {
                                inlineButtonList.Add(new InlineKeyboardButton[]
                                {
                                InlineKeyboardButton.WithCallbackData(password.login,$"login_{password.login}")
                                });
                            }

                            var inlineKeyboard = new InlineKeyboardMarkup(inlineButtonList);

                            await _botClient.SendTextMessageAsync(
                                update.CallbackQuery.Message.Chat.Id,
                                "Для данного сервиса есть несколько учетных записей:",
                                replyMarkup: inlineKeyboard);

                        }
                        else
                        {
                            var botMessage = await _botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id,
                                $"Login: {passwordList[0].login}\nPassword: {passwordList[0].decryptedPassword}");
                            Task.Run(async () =>
                                await _botUtils.DeleteMessageASync(_botClient, botMessage.Chat.Id, botMessage.MessageId, 10000));
                        }

                    }
                    else
                    {
                        var apiError =  await callbackResponse.Content.ReadFromJsonAsync<ApiError>();
                        await _botClient.SendTextMessageAsync(
                            update.CallbackQuery.Message.Chat.Id,
                            apiError.Message);
                    }
                }

                catch (Exception ex) 
                {
                    await _botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "BOT error:" + ex.Message);
                }
               
            }
        }

        public async Task GetPasswordByLoginAsync(Update update)
        {
            var callbackData = update.CallbackQuery.Data;
            var callBackId = update.CallbackQuery.From.Id;

            var login = callbackData.Substring("login_".Length);

            if (_passwordCache.TryGetValue(callBackId, out var passwordList))
            {
                var password = passwordList.FirstOrDefault(x => x.login == login);
                if (passwordList != null)
                {
                    var botMessage = await _botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id,
                        $"Login: {password.login}\nPassword: {password.decryptedPassword}");

                    
                    _passwordCache.Remove(callBackId);
                    Task.Run(async () => await _botUtils.DeleteMessageASync(_botClient, botMessage.Chat.Id, botMessage.MessageId, 10000));
                }
            }

        }
    }
}
