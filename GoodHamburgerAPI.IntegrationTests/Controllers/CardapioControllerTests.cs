using System.Net;
using System.Text.Json;
using GoodHamburgerAPI.Application.DTOs;
using GoodHamburgerAPI.Domain.Entities;
using GoodHamburgerAPI.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GoodHamburgerAPI.IntegrationTests.Controllers;

public class CardapioControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CardapioControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }


    private async Task ClearAndSeedDatabaseAsync(Func<AppDbContext, Task> seedAction)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync();

            var allItens = dbContext.ItensCardapio.ToList();
            dbContext.ItensCardapio.RemoveRange(allItens);

            var allTipos = dbContext.TipoItensCardapio.ToList();
            dbContext.TipoItensCardapio.RemoveRange(allTipos);

            await dbContext.SaveChangesAsync();
            await seedAction(dbContext);
        }
    }

    #region GetMenu Tests
    [Fact]
    public async Task GetMenu_WithoutFilter_ShouldReturnOkAndAllActiveItems()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            var tipo = new TipoItensCardapio { Nome = "Hamburguer", Ativo = true };
            dbContext.TipoItensCardapio.Add(tipo);
            await dbContext.SaveChangesAsync();

            var items = new[]
            {
                new ItensCardapio { Nome = "Hamburguer X", Preco = 15.00m, Ativo = true, TipoId = tipo.Id },
                new ItensCardapio { Nome = "Hamburguer G", Preco = 18.00m, Ativo = true, TipoId = tipo.Id }
            };
            dbContext.ItensCardapio.AddRange(items);
            await dbContext.SaveChangesAsync();
        });

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/menu");
        var content = await response.Content.ReadAsStringAsync();
        var menuItems = JsonSerializer.Deserialize<List<MenuItemDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(menuItems);
        Assert.Equal(2, menuItems.Count);
        Assert.All(menuItems, item => Assert.NotNull(item.Tipo));
    }
    [Fact]
    public async Task GetMenu_WithTypeFilter_ShouldReturnOkAndFilteredItems()
    {
        var tipoHamburguerIdValue = 0;
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            var tipoHamburguer = new TipoItensCardapio { Nome = "Hamburguer", Ativo = true };
            var tipoBebida = new TipoItensCardapio { Nome = "Bebida", Ativo = true };
            dbContext.TipoItensCardapio.AddRange(tipoHamburguer, tipoBebida);
            await dbContext.SaveChangesAsync();
            tipoHamburguerIdValue = tipoHamburguer.Id;

            var items = new[]
            {
                new ItensCardapio { Nome = "Hamburguer X", Preco = 15.00m, Ativo = true, TipoId = tipoHamburguer.Id },
                new ItensCardapio { Nome = "Coca Cola", Preco = 5.00m, Ativo = true, TipoId = tipoBebida.Id },
                new ItensCardapio { Nome = "Hamburguer G", Preco = 18.00m, Ativo = true, TipoId = tipoHamburguer.Id }
            };
            dbContext.ItensCardapio.AddRange(items);
            await dbContext.SaveChangesAsync();
        });

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/menu?tipoItem={tipoHamburguerIdValue}");
        var content = await response.Content.ReadAsStringAsync();
        var menuItems = JsonSerializer.Deserialize<List<MenuItemDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(menuItems);
        Assert.Equal(2, menuItems.Count);
        Assert.All(menuItems, item => Assert.Equal(tipoHamburguerIdValue, item.Tipo.Id));
    }
    [Fact]
    public async Task GetMenu_WithInactiveItems_ShouldOnlyReturnActiveItems()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            var tipo = new TipoItensCardapio { Nome = "Hamburguer", Ativo = true };
            dbContext.TipoItensCardapio.Add(tipo);
            await dbContext.SaveChangesAsync();

            var items = new[]
            {
                new ItensCardapio { Nome = "Ativo", Preco = 15.00m, Ativo = true, TipoId = tipo.Id },
                new ItensCardapio { Nome = "Inativo", Preco = 18.00m, Ativo = false, TipoId = tipo.Id }
            };
            dbContext.ItensCardapio.AddRange(items);
            await dbContext.SaveChangesAsync();
        });

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/menu");
        var content = await response.Content.ReadAsStringAsync();
        var menuItems = JsonSerializer.Deserialize<List<MenuItemDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(menuItems);
        Assert.Single(menuItems);
        Assert.Equal("Ativo", menuItems[0].Nome);
    }
    [Fact]
    public async Task GetMenu_WithNoItems_ShouldReturnEmptyList()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            await Task.CompletedTask;
        });

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/menu");
        var content = await response.Content.ReadAsStringAsync();
        var menuItems = JsonSerializer.Deserialize<List<MenuItemDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(menuItems);
        Assert.Empty(menuItems);
    }
    #endregion

    #region GetTiposItens Tests
    [Fact]
    public async Task GetTiposItens_ShouldReturnOkAndAllActiveTipos()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            var tipos = new[]
            {
                new TipoItensCardapio { Nome = "Hamburguer", Ativo = true },
                new TipoItensCardapio { Nome = "Bebida", Ativo = true },
                new TipoItensCardapio { Nome = "Sobremesa", Ativo = true }
            };
            dbContext.TipoItensCardapio.AddRange(tipos);
            await dbContext.SaveChangesAsync();
        });

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/tipoItem");
        var content = await response.Content.ReadAsStringAsync();
        var tipos = JsonSerializer.Deserialize<List<TipoItemDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(tipos);
        Assert.Equal(3, tipos.Count);
    }
    [Fact]
    public async Task GetTiposItens_WithInactiveTipos_ShouldOnlyReturnActiveTipos()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            var tipos = new[]
            {
                new TipoItensCardapio { Nome = "Ativo", Ativo = true },
                new TipoItensCardapio { Nome = "Inativo", Ativo = false }
            };
            dbContext.TipoItensCardapio.AddRange(tipos);
            await dbContext.SaveChangesAsync();
        });

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/tipoItem");
        var content = await response.Content.ReadAsStringAsync();
        var tipos = JsonSerializer.Deserialize<List<TipoItemDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(tipos);
        Assert.Single(tipos);
        Assert.Equal("Ativo", tipos[0].Nome);
    }
    [Fact]
    public async Task GetTiposItens_WithNoTipos_ShouldReturnEmptyList()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            await Task.CompletedTask;
        });

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/tipoItem");
        var content = await response.Content.ReadAsStringAsync();
        var tipos = JsonSerializer.Deserialize<List<TipoItemDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(tipos);
        Assert.Empty(tipos);
    }
    #endregion
}
