using GoodBeerBot.Api.Services;
using GoodBeerBot.Api.HostesServices;

namespace GoodBeerBot.Api.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<ITableService, GoogleTableService>();
        serviceCollection.AddTransient<ILeftoversService, LeftoversService>();

        serviceCollection.AddTransient<ITelegramBotService, TelegramBotService>();

        return serviceCollection;
    }
}