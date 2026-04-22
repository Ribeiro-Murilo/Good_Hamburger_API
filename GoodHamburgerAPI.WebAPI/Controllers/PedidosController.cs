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
            if (ex.Message == "item do pedido não encontrado no menu.")
            {
                return NotFound(new { mensagem = ex.Message });
            }

            if (ex.Message == "não é permitido adicionar o mesmo item mais de uma vez no pedido." ||
                ex.Message == "não é permitido mais de um item da mesma categoria no pedido.")
            {
                return BadRequest(new { mensagem = ex.Message });
            }

            throw;
        }
    }

    [HttpPost("api/pedidos/{id}/itens")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AdicionarItens(Guid id, [FromBody] PedidoRequestDto request)
    {
        try
        {
            await _pedidoService.AdicionarItensAsync(id, request);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message == "pedido não encontrado.")
            {
                return NotFound(new { mensagem = ex.Message });
            }

            if (ex.Message == "pedido já foi finalizado, não é possível fazer mais alterações.")
            {
                return BadRequest(new { mensagem = ex.Message });
            }

            if (ex.Message == "item do pedido não encontrado no menu.")
            {
                return NotFound(new { mensagem = ex.Message });
            }

            if (ex.Message == "não é permitido adicionar o mesmo item mais de uma vez no pedido." ||
                ex.Message == "não é permitido mais de um item da mesma categoria no pedido.")
            {
                return BadRequest(new { mensagem = ex.Message });
            }

            throw;
        }
    }

    [HttpDelete("api/pedidos/{id}/itens/{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoverItem(Guid id, int itemId)
    {
        try
        {
            await _pedidoService.RemoverItemAsync(id, itemId);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message == "pedido não encontrado.")
            {
                return NotFound(new { mensagem = ex.Message });
            }

            if (ex.Message == "pedido já foi finalizado, não é possível fazer mais alterações.")
            {
                return BadRequest(new { mensagem = ex.Message });
            }

            if (ex.Message == "item não encontrado no pedido.")
            {
                return NotFound(new { mensagem = ex.Message });
            }

            throw;
        }
    }

    [HttpPost("api/pedidos/{id}/fechar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> FecharPedido(Guid id)
    {
        try
        {
            await _pedidoService.FecharPedidoAsync(id);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message == "pedido não encontrado.")
            {
                return NotFound(new { mensagem = ex.Message });
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
            if (ex.Message == "pedido não encontrado.")
            {
                return NotFound(new { mensagem = ex.Message });
            }

            throw;
        }
    }

    [HttpDelete("api/pedidos/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeletarPedido(Guid id)
    {
        try
        {
            await _pedidoService.DeletarPedidoAsync(id);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message == "pedido não encontrado.")
            {
                return NotFound(new { mensagem = ex.Message });
            }

            throw;
        }
    }
}
