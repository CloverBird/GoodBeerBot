using GoodBeerBot.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace GoodBeerBot.Api.Controllers;

[ApiController]
[Route("api/bot")]
public class TelegramBotController : ControllerBase
{
    private readonly ILogger<TelegramBotController> _logger;
    private readonly ITelegramBotService _telegramBotService;

    public TelegramBotController(ILogger<TelegramBotController> logger,
                                 ITelegramBotService telegramBotService)
    {
        _logger = logger;
        _telegramBotService = telegramBotService;
    }

    [HttpPost("update")]
    public async Task<IActionResult> Post([FromBody] Update update)
    {
        try
        {
            if (update is null)
            {
                _logger.LogWarning("Null update received");
                return Ok();
            }

            _logger.LogInformation("Update {Id} type {Type}", update.Id, update.Type);

            await _telegramBotService.ProcessUpdateAsync(update);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing update");
            return Ok();
        }
    }
}