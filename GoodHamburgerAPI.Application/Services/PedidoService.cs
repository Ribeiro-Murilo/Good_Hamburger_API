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
        var itensDicionario = new Dictionary<int, int>();

        foreach (var item in request.Itens)
        {
            if (item.Quantidade <= 0)
            {
                throw new InvalidOperationException("quantidade dos itens do pedido deve ser maior que zero.");
            }

            if (itensDicionario.ContainsKey(item.Id))
            {
                itensDicionario[item.Id] += item.Quantidade;
            }
            else
            {
                itensDicionario[item.Id] = item.Quantidade;
            }
        }

        var itensComDetalhes = new List<ItemPedido>();
        decimal valorTotal = 0;

        foreach (var itemId in itensDicionario.Keys)
        {
            var item = await _context.ItensCardapio.FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
            {
                throw new InvalidOperationException("item do pedido não encontrado no menu.");
            }

            if (!item.Ativo)
            {
                throw new InvalidOperationException("item do pedido não encontrado no menu.");
            }

            itensComDetalhes.Add(new ItemPedido
            {
                Id = item.Id,
                Nome = item.Nome,
                Valor = item.Preco,
                Quantidade = itensDicionario[itemId]
            });

            valorTotal += item.Preco * itensDicionario[itemId];
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

    public async Task AddItensAsync(Guid pedidoId, PedidoRequestDto request)
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

        var itensDicionario = new Dictionary<int, int>();

        foreach (var item in request.Itens)
        {
            if (item.Quantidade <= 0)
            {
                throw new InvalidOperationException("quantidade dos itens do pedido deve ser maior que zero.");
            }

            itensDicionario[item.Id] = item.Quantidade;
        }

        var itensAtualizados = new List<ItemPedido>();
        decimal valorTotal = 0;

        foreach (var itemId in itensDicionario.Keys)
        {
            var item = await _context.ItensCardapio.FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
            {
                throw new InvalidOperationException("item do pedido não encontrado no menu.");
            }

            if (!item.Ativo)
            {
                throw new InvalidOperationException("item do pedido não encontrado no menu.");
            }

            itensAtualizados.Add(new ItemPedido
            {
                Id = item.Id,
                Nome = item.Nome,
                Valor = item.Preco,
                Quantidade = itensDicionario[itemId]
            });

            valorTotal += item.Preco * itensDicionario[itemId];
        }

        pedido.Itens = itensAtualizados;
        pedido.AtualizadoEm = DateTime.UtcNow;
        pedido.ValorSemDesconto = valorTotal;
        pedido.ValorFinal = valorTotal;

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
            item.ValorUnitario = item.Valor;
            item.ValorTotal = item.Valor * item.Quantidade;
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

            var valorTotal = pedidoBanco.Itens.Sum(i => i.ValorTotal);

            return new PedidoGetResponseDto
            {
                Id = pedidoBanco.Id,
                Itens = pedidoBanco.Itens.Select(i => new ItemPedidoResponseDto
                {
                    Id = i.Id,
                    Nome = i.Nome,
                    Valor = i.ValorUnitario,
                    Quantidade = i.Quantidade
                }).ToList(),
                ComDesconto = pedidoBanco.ComDesconto,
                ValorTotal = valorTotal,
                ValorFinal = pedidoBanco.ValorFinal,
                ValorComDesconto = pedidoBanco.ComDesconto
            };
        }

        var valorTotalRedis = pedido.Itens.Sum(i => i.Valor * i.Quantidade);

        return new PedidoGetResponseDto
        {
            Id = pedido.Id,
            Itens = pedido.Itens.Select(i => new ItemPedidoResponseDto
            {
                Id = i.Id,
                Nome = i.Nome,
                Valor = i.Valor,
                Quantidade = i.Quantidade
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
