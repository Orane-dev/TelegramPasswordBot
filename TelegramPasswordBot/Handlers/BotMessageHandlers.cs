using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramPasswordBot.DTO;
using TelegramPasswordBot.Interfaces;
using TelegramPasswordBot.Utils;

namespace TelegramPasswordBot.Handlers
{
    public class BotMessageHandlers
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ITelegramBotUtils _botUtils;
        public BotMessageHandlers(ITelegramBotClient botClient, IHttpClientFactory clientFactory, ITelegramBotUtils botUtils)
        {
            _botClient = botClient;
            _clientFactory = clientFactory;
            _botUtils = botUtils;

        }

        public async Task RegisterUserHandlerAsync(Update update)
        {
            try
            {
                var massage = update.Message;
                var userId = update.Message.From.Id;

                var separatedComand = update.Message.Text.Split(" ");
                if (separatedComand.Length != 2)
                {
                    await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "Incorrect input data");
                    return;
                }

                if (_botUtils.GetUserRole(userId.ToString()) == 0)
                {
                    await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "You don`t have permission for this action");
                    return;
                }

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("requestTelegramId", userId.ToString()),
                    new KeyValuePair<string, string>("telegramId", separatedComand[1])
                });

                using var client = _clientFactory.CreateClient();
                var response = await client.PostAsync($"https://{_botUtils.apiEndPoint}/api/Managment", content);

                if (response.IsSuccessStatusCode)
                {
                    var newUser = await response.Content.ReadFromJsonAsync<TelegramUserDTO>();
                    _botUtils.AddToAllowedIds(newUser);
                    await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "User created");
                }
                else
                {
                    var apiError = await response.Content.ReadFromJsonAsync<ApiError>();
                    await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "API error: " + apiError.Message);
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "BOT error: " + ex.Message);
            }
        }
        public async Task CreatePasswordHandlerAsync(Update update)
        {
            var message = update.Message;
            var userId = message.From.Id;

            var splitedCommand = message.Text.Split(' ');
            if (splitedCommand.Length == 4)
            {
                string service = splitedCommand[1];
                string login = splitedCommand[2];
                string password = splitedCommand[3];

                var postContent = new CreatePasswordDTO
                {
                    id = 0,
                    telegramUserId = userId.ToString(),
                    serviceName = service,
                    login = login,
                    decryptedPassword = password,
                    createTime = DateTime.UtcNow,
                };

                var jsonContent = JsonSerializer.Serialize(postContent);
                HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                try
                {
                    using var client = _clientFactory.CreateClient();
                    var response = await client.PostAsync($"https://{_botUtils.apiEndPoint}/api/Password", content);
                    if (response.IsSuccessStatusCode)
                    {
                        await _botClient.SendTextMessageAsync(message.Chat, $"Password for {service} service created sucessfuly");
                    }
                    else
                    {
                        var apiError = await response.Content.ReadFromJsonAsync<ApiError>();
                        await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "API error: " + apiError.Message);
                    }
                }
                catch (Exception ex)
                {
                    await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "BOT error: " + ex.Message);
                }
                finally
                {
                    Task.Run(async () =>
                        await _botUtils.DeleteMessageASync(_botClient, message.Chat.Id, message.MessageId, 0));
                }
            }
            else
            {
                await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "Incorrect input data");
            }

        }

        public async Task GetUserServicesHandlerAsync(Update update)
        {
            var message = update.Message;

            using var client = _clientFactory.CreateClient();
            try
            {
                var response = await client.GetAsync($"https://{_botUtils.apiEndPoint}/api/Service?telegramId={message.From.Id}");
                if (response.IsSuccessStatusCode)
                {
                    var inlineButtonList = new List<InlineKeyboardButton[]>();
                    var content = await response.Content.ReadFromJsonAsync<Dictionary<int, string>>();
                    foreach (var (k, v) in content)
                    {
                        inlineButtonList.Add(new InlineKeyboardButton[]
                        {
                                        InlineKeyboardButton.WithCallbackData(v, $"service_{v}")
                        });
                    }

                    var inlineKeyboard = new InlineKeyboardMarkup(inlineButtonList);
                    await _botClient.SendTextMessageAsync(message.Chat.Id, "Services: ", replyMarkup: inlineKeyboard);
                }
                else
                {
                    var apiError = await response.Content.ReadFromJsonAsync<ApiError>();
                    await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "API error: " + apiError.Message);
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "BOT error: " + ex.Message);
            }
        }

        public async Task DeletePasswordHandlerAsync(Update update)
        {
            var message = update.Message;
            var separatedMessage = message.Text.Split(" ");
            if (separatedMessage.Length != 3)
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, $"Incorrect input values");
                return;
            }
            var service = separatedMessage[1];
            var login = separatedMessage[2];

            try
            {
                using var client = _clientFactory.CreateClient();
                var response = await client.DeleteAsync(
                    $"https://{_botUtils.apiEndPoint}/api/Password?telegramId={message.From.Id}&service={service}&login={login}");
                if (response.IsSuccessStatusCode)
                {
                    await _botClient.SendTextMessageAsync(message.Chat.Id, $"{service} service password deleted");
                }
                else
                {
                    var apiError = await response.Content.ReadFromJsonAsync<ApiError>();
                    await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "API error: " + apiError.Message);
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "BOT error:" + ex.Message);
            }
            finally
            {
                Task.Run(async () =>
                    await _botUtils.DeleteMessageASync(_botClient, message.Chat.Id, message.MessageId, 1));
            }
        }

        public async Task PutPasswordHandlerAsync(Update update)
        {
            var message = update.Message;
            var separatedMessage = message.Text.Split(" ");
            if (separatedMessage.Length != 4)
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, $"Incorrect input values");
                return;
            }
            var service = separatedMessage[1];
            var login = separatedMessage[2];
            var updatePassword = separatedMessage[3];

            var password = new CreatePasswordDTO()
            {
                id = 0,
                telegramUserId = message.From.Id.ToString(),
                login = login,
                serviceName = service,
                decryptedPassword = updatePassword,
                createTime = DateTime.UtcNow,
            };

            var jsonContent = JsonSerializer.Serialize<CreatePasswordDTO>(password);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            try
            {
                using var client = _clientFactory.CreateClient();
                var response = await client.PutAsync($"https://{_botUtils.apiEndPoint}/api/Password", httpContent);

                if (response.IsSuccessStatusCode)
                {
                    await _botClient.SendTextMessageAsync(message.Chat.Id, $"{service} service password updated");
                }
                else
                {
                    var apiError = await response.Content.ReadFromJsonAsync<ApiError>();
                    await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "API error: " + apiError.Message);
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "BOT error:" + ex.Message);
            }
            finally
            {
                Task.Run(async () =>
                    await _botUtils.DeleteMessageASync(_botClient, message.Chat.Id, message.MessageId, 1));
            }

        }
    }
}
