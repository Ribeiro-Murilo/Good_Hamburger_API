using GoodHamburgerAPI.Application.DTOs;
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
        var itensDicionario = new Dictionary<int, int>();

        foreach (var item in request.Itens)
        {
            if (itensDicionario.ContainsKey(item.Id))
            {
                itensDicionario[item.Id] += item.Quantidade;
            }
            else
            {
                itensDicionario[item.Id] = item.Quantidade;
            }
        }

        foreach (var itemId in itensDicionario.Keys)
        {
            var item = await _context.ItensCardapio.FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
            {
                throw new InvalidOperationException("recurso não encontrado.");
            }

            if (!item.Ativo)
            {
                throw new InvalidOperationException("pedido inválido.");
            }
        }

        var pedidoId = Guid.NewGuid();

        var itensRedis = itensDicionario.Select(kvp => new ItemPedido
        {
            Id = kvp.Key,
            Quantidade = kvp.Value
        }).ToList();

        var pedidoRedis = new Pedido
        {
            Id = pedidoId,
            Itens = itensRedis,
            ComDesconto = false
        };

        await _redisService.SetAsync($"pedido:{pedidoId}", pedidoRedis);

        return new PedidoResponseDto { Id = pedidoId };
    }

    public async Task AddItensAsync(Guid pedidoId, PedidoRequestDto request)
    {
        var pedido = await _redisService.GetAsync<Pedido>($"pedido:{pedidoId}");

        if (pedido == null)
        {
            throw new InvalidOperationException("recurso não encontrado.");
        }

        var itensDicionario = pedido.Itens.ToDictionary(i => i.Id, i => i.Quantidade);

        foreach (var item in request.Itens)
        {
            if (itensDicionario.ContainsKey(item.Id))
            {
                itensDicionario[item.Id] += item.Quantidade;
            }
            else
            {
                itensDicionario[item.Id] = item.Quantidade;
            }
        }

        foreach (var itemId in itensDicionario.Keys)
        {
            var item = await _context.ItensCardapio.FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
            {
                throw new InvalidOperationException("recurso não encontrado.");
            }

            if (!item.Ativo)
            {
                throw new InvalidOperationException("pedido inválido.");
            }
        }

        var itensAtualizados = itensDicionario.Select(kvp => new ItemPedido
        {
            Id = kvp.Key,
            Quantidade = kvp.Value
        }).ToList();

        pedido.Itens = itensAtualizados;

        await _redisService.SetAsync($"pedido:{pedidoId}", pedido);
    }

    private class Pedido
    {
        public Guid Id { get; set; }
        public List<ItemPedido> Itens { get; set; } = [];
        public bool ComDesconto { get; set; }
    }

    private class ItemPedido
    {
        public int Id { get; set; }
        public int Quantidade { get; set; }
    }
}
