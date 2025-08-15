namespace GoodBeerBot.Api.Configurations;

public class TelegramBotConfiguration
{
    public string Token { get; set; }

    public string Webhook {  get; set; }

    public long AdminChatId { get; set; }

    public long EmployeeChatId { get; set; }
}