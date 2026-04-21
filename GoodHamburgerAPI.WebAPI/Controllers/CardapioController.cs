using GoodHamburgerAPI.Application.DTOs;
using GoodHamburgerAPI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoodHamburgerAPI.WebAPI.Controllers;

[ApiController]
public class CardapioController : ControllerBase
{
    private readonly ICardapioService _cardapioService;

    public CardapioController(ICardapioService cardapioService)
    {
        _cardapioService = cardapioService;
    }

    [HttpGet("api/menu")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MenuItemDto>>> GetMenu([FromQuery] int? tipoItem)
    {
        var menu = await _cardapioService.GetMenuAsync(tipoItem);
        return Ok(menu);
    }

    [HttpGet("api/tipoItem")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TipoItemDto>>> GetTiposItens()
    {
        var tipos = await _cardapioService.GetTiposItensAsync();
        return Ok(tipos);
    }
}
