using GoodHamburgerAPI.Application.DTOs;
using GoodHamburgerAPI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoodHamburgerAPI.WebAPI.Controllers;

[ApiController]
public class PedidosController : ControllerBase
{
    private readonly IPedidoService _pedidoService;

    public PedidosController(IPedidoService pedidoService)
    {
        _pedidoService = pedidoService;
    }

    [HttpPost("api/pedidos")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoResponseDto>> CreatePedido([FromBody] PedidoRequestDto request)
    {
        try
        {
            var response = await _pedidoService.CreatePedidoAsync(request);
            return CreatedAtAction(nameof(CreatePedido), new { id = response.Id }, response);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message == "recurso não encontrado.")
            {
                return NotFound(new { mensagem = ex.Message });
            }

            if (ex.Message == "pedido inválido.")
            {
                return BadRequest(new { mensagem = ex.Message });
            }

            throw;
        }
    }
}
