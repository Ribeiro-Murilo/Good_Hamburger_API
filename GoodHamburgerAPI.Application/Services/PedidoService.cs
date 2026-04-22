using GoodHamburgerAPI.Application.DTOs;
using GoodHamburgerAPI.Domain.Entities;
using GoodHamburgerAPI.Infrastructure.Cache;
using GoodHamburgerAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoodHamburgerAPI.Application.Services;

public class PedidoService : IPedidoService
{
    private readonly AppDbContext _context;
    private readonly IRedisService _redisService;

    public PedidoService(AppDbContext context, IRedisService redisService)
    {
        _context = context;
        _redisService = redisService;
    }

    public async Task<PedidoResponseDto> CreatePedidoAsync(PedidoRequestDto request)
    {
        var idsVistos = new HashSet<int>();
        var tiposVistos = new HashSet<int>();
        var itensComDetalhes = new List<ItemPedido>();
        decimal valorTotal = 0;

        foreach (var itemRequest in request.Itens)
        {
            if (idsVistos.Contains(itemRequest.Id))
            {
                throw new InvalidOperationException("não é permitido adicionar o mesmo item mais de uma vez no pedido.");
            }

            var item = await _context.ItensCardapio.FirstOrDefaultAsync(i => i.Id == itemRequest.Id);

            if (item == null)
            {
                throw new InvalidOperationException("item do pedido não encontrado no menu.");
            }

            if (!item.Ativo)
            {
                throw new InvalidOperationException("item do pedido não encontrado no menu.");
            }

            if (tiposVistos.Contains(item.TipoId))
            {
                throw new InvalidOperationException("não é permitido mais de um item da mesma categoria no pedido.");
            }

            idsVistos.Add(itemRequest.Id);
            tiposVistos.Add(item.TipoId);

            itensComDetalhes.Add(new ItemPedido
            {
                Id = item.Id,
                TipoId = item.TipoId,
                Nome = item.Nome,
                Valor = item.Preco
            });

            valorTotal += item.Preco;
        }

        var pedidoId = Guid.NewGuid();

        var pedidoRedis = new Pedido
        {
            Id = pedidoId,
            Itens = itensComDetalhes,
            ComDesconto = false,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow,
            ValorSemDesconto = valorTotal,
            ValorFinal = valorTotal
        };

        await _redisService.SetAsync($"pedido:{pedidoId}", pedidoRedis);

        return new PedidoResponseDto { Id = pedidoId };
    }

    public async Task RemoverItemAsync(Guid pedidoId, int itemId)
    {
        var pedido = await _redisService.GetAsync<Pedido>($"pedido:{pedidoId}");

        if (pedido == null)
        {
            var pedidoBanco = await _context.Pedido.FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedidoBanco != null)
            {
                throw new InvalidOperationException("pedido já foi finalizado, não é possível fazer mais alterações.");
            }

            throw new InvalidOperationException("pedido não encontrado.");
        }

        var item = pedido.Itens.FirstOrDefault(i => i.Id == itemId);

        if (item == null)
        {
            throw new InvalidOperationException("item não encontrado no pedido.");
        }

        pedido.Itens.Remove(item);
        pedido.AtualizadoEm = DateTime.UtcNow;
        pedido.ValorSemDesconto = pedido.Itens.Sum(i => i.Valor);
        pedido.ValorFinal = pedido.ValorSemDesconto;

        await _redisService.SetAsync($"pedido:{pedidoId}", pedido);
    }

    public async Task FecharPedidoAsync(Guid pedidoId)
    {
        var pedido = await _redisService.GetAsync<Pedido>($"pedido:{pedidoId}");

        if (pedido == null)
        {
            throw new InvalidOperationException("pedido não encontrado.");
        }

        foreach (var item in pedido.Itens)
        {
            item.PedidoId = pedido.Id;
        }

        _context.Pedido.Add(pedido);
        await _context.SaveChangesAsync();

        await _redisService.RemoveAsync($"pedido:{pedidoId}");
    }

    public async Task<PedidoGetResponseDto> GetPedidoAsync(Guid pedidoId)
    {
        var pedido = await _redisService.GetAsync<Pedido>($"pedido:{pedidoId}");

        if (pedido == null)
        {
            var pedidoBanco = await _context.Pedido
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == pedidoId && p.Ativo);

            if (pedidoBanco == null)
            {
                throw new InvalidOperationException("pedido não encontrado.");
            }

            var valorTotal = pedidoBanco.Itens.Sum(i => i.Valor);

            return new PedidoGetResponseDto
            {
                Id = pedidoBanco.Id,
                Itens = pedidoBanco.Itens.Select(i => new ItemPedidoResponseDto
                {
                    Id = i.Id,
                    Nome = i.Nome,
                    Valor = i.Valor
                }).ToList(),
                ComDesconto = pedidoBanco.ComDesconto,
                ValorTotal = valorTotal,
                ValorFinal = pedidoBanco.ValorFinal,
                ValorComDesconto = pedidoBanco.ComDesconto
            };
        }

        var valorTotalRedis = pedido.Itens.Sum(i => i.Valor);

        return new PedidoGetResponseDto
        {
            Id = pedido.Id,
            Itens = pedido.Itens.Select(i => new ItemPedidoResponseDto
            {
                Id = i.Id,
                Nome = i.Nome,
                Valor = i.Valor
            }).ToList(),
            ComDesconto = pedido.ComDesconto,
            ValorTotal = valorTotalRedis,
            ValorFinal = pedido.ValorFinal,
            ValorComDesconto = pedido.ComDesconto
        };
    }

    public async Task DeletarPedidoAsync(Guid pedidoId)
    {
        var pedidoRedis = await _redisService.GetAsync<Pedido>($"pedido:{pedidoId}");

        if (pedidoRedis != null)
        {
            await _redisService.RemoveAsync($"pedido:{pedidoId}");
            return;
        }

        var pedidoBanco = await _context.Pedido.FirstOrDefaultAsync(p => p.Id == pedidoId);

        if (pedidoBanco == null)
        {
            throw new InvalidOperationException("pedido não encontrado.");
        }

        pedidoBanco.Ativo = false;
        pedidoBanco.DataExclusao = DateTime.UtcNow;

        _context.Pedido.Update(pedidoBanco);
        await _context.SaveChangesAsync();
    }

}
