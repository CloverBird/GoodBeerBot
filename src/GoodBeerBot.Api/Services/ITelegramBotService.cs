using Telegram.Bot.Types;

namespace GoodBeerBot.Api.Services;

public interface ITelegramBotService
{
    Task ProcessUpdateAsync(Update update);     
    Task SendTextAsync(long chatId, string text);   
    Task SendHtmlAsync(long chatId, string html);
    Task SendExpiryNotificationsAsync();

    Task ClearReportsSheet();
}