using GoodBeerBot.Api.Configurations;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using System.Reflection;

namespace GoodBeerBot.Api.Extensions;

public static class GoogleSheetsExtensions
{
    private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };

    public static IServiceCollection AddGoogleServices(this IServiceCollection serviceCollection)
    {
        GoogleCredential cred;
        using (var stream = File.OpenRead(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "google.Credentials.json")))
        {
            cred = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
        }

        serviceCollection.AddSingleton(new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = cred,
            ApplicationName = "GoodBeerBot"
        }));

        return serviceCollection;
    }
}