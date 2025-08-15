namespace GoodBeerBot.Api.Configurations;

public class AppConfiguration
{
    public TelegramBotConfiguration TelegramBot { get; set; }

    public GoogleTablesConfiguration GoogleTables { get; set; }
}