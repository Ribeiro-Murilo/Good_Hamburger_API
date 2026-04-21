using GoodHamburgerAPI.Application.Services;
using GoodHamburgerAPI.Domain.Entities;
using GoodHamburgerAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GoodHamburgerAPI.UnitTests.Services;

public class CardapioServiceTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    #region GetMenuAsync Tests

    [Fact]
    public async Task GetMenuAsync_WithoutFilter_ShouldReturnAllActiveMenuItems()
    {
        using (var context = CreateInMemoryDbContext())
        {
            var tipoItem = new TipoItensCardapio { Nome = "Hamburguer", Ativo = true };
            context.TipoItensCardapio.Add(tipoItem);
            await context.SaveChangesAsync();

            var menuItems = new[]
            {
                new ItensCardapio { Nome = "Hamburguer X", Preco = 15.00m, Ativo = true, TipoId = tipoItem.Id },
                new ItensCardapio { Nome = "Hamburguer G", Preco = 18.00m, Ativo = true, TipoId = tipoItem.Id }
            };
            context.ItensCardapio.AddRange(menuItems);
            await context.SaveChangesAsync();

            var service = new CardapioService(context);

            var result = await service.GetMenuAsync();

            Assert.NotEmpty(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Hamburguer X", result[0].Nome);
            Assert.Equal(15.00m, result[0].Preco);
        }
    }

    [Fact]
    public async Task GetMenuAsync_WithTypeFilter_ShouldReturnFilteredMenuItems()
    {
        using (var context = CreateInMemoryDbContext())
        {
            var tipoHamburguer = new TipoItensCardapio { Nome = "Hamburguer", Ativo = true };
            var tipoBebida = new TipoItensCardapio { Nome = "Bebida", Ativo = true };
            context.TipoItensCardapio.AddRange(tipoHamburguer, tipoBebida);
            await context.SaveChangesAsync();

            var menuItems = new[]
            {
                new ItensCardapio { Nome = "Hamburguer X", Preco = 15.00m, Ativo = true, TipoId = tipoHamburguer.Id },
                new ItensCardapio { Nome = "Coca Cola", Preco = 5.00m, Ativo = true, TipoId = tipoBebida.Id },
                new ItensCardapio { Nome = "Hamburguer G", Preco = 18.00m, Ativo = true, TipoId = tipoHamburguer.Id }
            };
            context.ItensCardapio.AddRange(menuItems);
            await context.SaveChangesAsync();

            var service = new CardapioService(context);

            var result = await service.GetMenuAsync(tipoHamburguer.Id);

            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.Equal(tipoHamburguer.Id, item.Tipo.Id));
        }
    }

    [Fact]
    public async Task GetMenuAsync_WithInactiveItems_ShouldOnlyReturnActiveItems()
    {
        using (var context = CreateInMemoryDbContext())
        {
            var tipoItem = new TipoItensCardapio { Nome = "Hamburguer", Ativo = true };
            context.TipoItensCardapio.Add(tipoItem);
            await context.SaveChangesAsync();

            var menuItems = new[]
            {
                new ItensCardapio { Nome = "Ativo", Preco = 15.00m, Ativo = true, TipoId = tipoItem.Id },
                new ItensCardapio { Nome = "Inativo", Preco = 18.00m, Ativo = false, TipoId = tipoItem.Id }
            };
            context.ItensCardapio.AddRange(menuItems);
            await context.SaveChangesAsync();

            var service = new CardapioService(context);

            var result = await service.GetMenuAsync();

            Assert.Single(result);
            Assert.Equal("Ativo", result[0].Nome);
        }
    }

    [Fact]
    public async Task GetMenuAsync_WithNoResults_ShouldReturnEmptyList()
    {
        using (var context = CreateInMemoryDbContext())
        {
            var service = new CardapioService(context);

            var result = await service.GetMenuAsync();

            Assert.Empty(result);
        }
    }

    #endregion

    #region GetTiposItensAsync Tests

    [Fact]
    public async Task GetTiposItensAsync_ShouldReturnAllActiveTipos()
    {
        using (var context = CreateInMemoryDbContext())
        {
            var tipos = new[]
            {
                new TipoItensCardapio { Nome = "Hamburguer", Ativo = true },
                new TipoItensCardapio { Nome = "Bebida", Ativo = true },
                new TipoItensCardapio { Nome = "Sobremesa", Ativo = true }
            };
            context.TipoItensCardapio.AddRange(tipos);
            await context.SaveChangesAsync();

            var service = new CardapioService(context);

            var result = await service.GetTiposItensAsync();

            Assert.Equal(3, result.Count);
            Assert.Contains(result, t => t.Nome == "Hamburguer");
            Assert.Contains(result, t => t.Nome == "Bebida");
        }
    }

    [Fact]
    public async Task GetTiposItensAsync_WithInactiveTipos_ShouldOnlyReturnActiveTipos()
    {
        using (var context = CreateInMemoryDbContext())
        {
            var tipos = new[]
            {
                new TipoItensCardapio { Nome = "Ativo", Ativo = true },
                new TipoItensCardapio { Nome = "Inativo", Ativo = false }
            };
            context.TipoItensCardapio.AddRange(tipos);
            await context.SaveChangesAsync();

            var service = new CardapioService(context);

            var result = await service.GetTiposItensAsync();

            Assert.Single(result);
            Assert.Equal("Ativo", result[0].Nome);
        }
    }

    [Fact]
    public async Task GetTiposItensAsync_WithNoResults_ShouldReturnEmptyList()
    {
        using (var context = CreateInMemoryDbContext())
        {
            var service = new CardapioService(context);

            var result = await service.GetTiposItensAsync();

            Assert.Empty(result);
        }
    }

    #endregion
}
