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

    [HttpPut("api/pedidos/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AddItens(Guid id, [FromBody] PedidoRequestDto request)
    {
        try
        {
            await _pedidoService.AddItensAsync(id, request);
            return NoContent();
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

    [HttpGet("api/pedidos/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoGetResponseDto>> GetPedido(Guid id)
    {
        try
        {
            var response = await _pedidoService.GetPedidoAsync(id);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message == "recurso não encontrado.")
            {
                return NotFound(new { mensagem = ex.Message });
            }

            throw;
        }
    }
}
