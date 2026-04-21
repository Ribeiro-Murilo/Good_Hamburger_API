using GoodHamburgerAPI.Application.DTOs;
using GoodHamburgerAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoodHamburgerAPI.Application.Services;

public class CardapioService : ICardapioService
{
    private readonly AppDbContext _context;

    public CardapioService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MenuItemDto>> GetMenuAsync(int? tipoItemId = null)
    {
        IQueryable<Domain.Entities.ItensCardapio> query = _context.ItensCardapio
            .Include(i => i.Tipo)
            .Where(i => i.Ativo);

        if (tipoItemId.HasValue)
        {
            query = query.Where(i => i.TipoId == tipoItemId.Value);
        }

        var itens = await query.ToListAsync();

        return itens.Select(i => new MenuItemDto
        {
            Id = i.Id,
            Nome = i.Nome,
            Preco = i.Preco,
            Tipo = new TipoItemDto
            {
                Id = i.Tipo!.Id,
                Nome = i.Tipo.Nome
            }
        }).ToList();
    }

    public async Task<List<TipoItemDto>> GetTiposItensAsync()
    {
        var tipos = await _context.TipoItensCardapio
            .Where(t => t.Ativo)
            .ToListAsync();

        return tipos.Select(t => new TipoItemDto
        {
            Id = t.Id,
            Nome = t.Nome
        }).ToList();
    }
}
