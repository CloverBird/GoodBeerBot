using GodBeerBot.Api.Services;

namespace GodBeerBot.Api.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<ITableService, GoogleTableService>();
        serviceCollection.AddTransient<ILeftoversService, LeftoversService>();

        serviceCollection.AddScoped<ITelegramBotService, TelegramBotService>();

        return serviceCollection;
    }
}