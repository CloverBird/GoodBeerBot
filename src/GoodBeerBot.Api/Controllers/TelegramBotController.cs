using GoodBeerBot.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace GoodBeerBot.Api.Controllers;

[ApiController]
[Route("api/bot")]
public class TelegramBotController : ControllerBase
{
    private readonly Logger<TelegramBotController> _logger;
    private readonly ITelegramBotService _telegramBotService;

    public TelegramBotController(Logger<TelegramBotController> logger,
                                 ITelegramBotService telegramBotService)
    {
        _logger = logger;
        _telegramBotService = telegramBotService;
    }

    [HttpPost("update")]
    public async Task<IActionResult> Post([FromBody] Update update)
    {
        await _telegramBotService.ProcessUpdateAsync(update); // твоя логіка
        return Ok(); // важливо: 200 OK швидко
    }
}