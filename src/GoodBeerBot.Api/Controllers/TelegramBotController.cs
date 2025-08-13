using GodBeerBot.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GodBeerBot.Api.Controllers;

[ApiController]
[Route("api/[contoller]")]
public class TelegramBotController : ControllerBase
{
    private readonly Logger<TelegramBotController> _logger;
    private readonly ILeftoversService _leftoversService;

    public TelegramBotController(Logger<TelegramBotController> logger, 
                                 ILeftoversService leftoversService)
    {
        _logger = logger;
        _leftoversService = leftoversService;
    }
}