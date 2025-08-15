using GoodBeerBot.Api.Configurations;
using Telegram.Bot;

namespace GoodBeerBot.Api.Extensions;

    public static class TelegramBotExtensions
    {
        public async static Task<IServiceCollection> AddTelegramServices(this IServiceCollection serviceCollection,
                                                                         TelegramBotConfiguration telegramBotConfiguration)
        {
            var botClient = new TelegramBotClient(telegramBotConfiguration.Token);
            await botClient.SetWebhook(telegramBotConfiguration.Webhook);

            serviceCollection.AddSingleton<ITelegramBotClient>(botClient);
            return serviceCollection;
        }
    }