
using Telegram.Bot;
using TelegramPasswordBot.DTO;
using System.Net.Http.Json;
using TelegramPasswordBot.Interfaces;

namespace TelegramPasswordBot.Utils
{
    public class TelegramBotUtils : ITelegramBotUtils
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public Dictionary<string, int> AllowedIds { get; set; } = new Dictionary<string, int>();
        public string apiEndPoint { get; private set; }
        public TelegramBotUtils(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
#if DEBUG
            apiEndPoint = Environment.GetEnvironmentVariable("DEBUG");
#else
            apiEndPoint =  Environment.GetEnvironmentVariable("RELEASE");
#endif
        }

        public async Task<List<TelegramUserDTO>> GetAllowedUsers()
        {
            while (true)
            {
                try
                {
                    using var client = _httpClientFactory.CreateClient();
                    HttpResponseMessage response = await client.GetAsync($"https://{apiEndPoint}/Managment/GetAllUsers");
                    if (response.IsSuccessStatusCode)
                    {
                        List<TelegramUserDTO> telegramUsers = await response.Content.ReadFromJsonAsync<List<TelegramUserDTO>>();
                        return telegramUsers;
                    }
                    return new List<TelegramUserDTO>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Cannot connect to password API");
                    await Task.Delay(5000);
                }
            }
        }

        public void AddToAllowedIds(List<TelegramUserDTO> telegramUsers)
        {
            foreach (var telegramUser in telegramUsers)
            {
                AllowedIds[telegramUser.telegramId] = telegramUser.role;
            }
        }

        public void AddToAllowedIds(TelegramUserDTO telegramUser)
        {

            if (AllowedIds.ContainsKey(telegramUser.telegramId))
            {
                AllowedIds[telegramUser.telegramId] = telegramUser.role;
            }
            else
            {
                throw new Exception($"User {telegramUser.telegramId} already inrolled");
            }
        }

        public async Task DeleteMessageASync(ITelegramBotClient botclient, long chatId, int messageId, int delay)
        {
            await Task.Delay(delay);
            await botclient.DeleteMessageAsync(chatId, messageId);
        }

        public int GetUserRole(string telegramId)
        {
            AllowedIds.TryGetValue(telegramId, out var role);
            return role;
        }
    }
}
