using GoodBeerBot.Api.Services;

namespace GoodBeerBot.Api.HostesServices;

public class ExpiryNotificationService : BackgroundService
{
    private readonly ITelegramBotService _botService;
    private readonly ILogger<ExpiryNotificationService> _logger;

    public ExpiryNotificationService(ITelegramBotService botService, ILogger<ExpiryNotificationService> logger)
    {
        _botService = botService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _botService.ClearReportsSheet();

                await _botService.SendExpiryNotificationsAsync();
                _logger.LogInformation("Expiry notifications sent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending expiry notifications.");
            }

            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
