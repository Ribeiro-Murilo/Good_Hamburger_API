using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GoodHamburgerAPI.Application.DTOs;
using GoodHamburgerAPI.Domain.Entities;
using GoodHamburgerAPI.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;

namespace GoodHamburgerAPI.IntegrationTests.Controllers;

[CollectionDefinition("Pedidos Controller Tests", DisableParallelization = true)]
public class PedidosControllerTestsCollection
{
}

[Collection("Pedidos Controller Tests")]
public class PedidosControllerTests : IAsyncLifetime
{
    private CustomWebApplicationFactory _factory = null!;

    public async Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory();
        await _factory.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
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


    #region POST /api/pedidos Tests

    [Fact]
    public async Task CreatePedido_WithValidItems_ShouldReturnCreatedAndId()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            var tipo = new TipoItensCardapio { Nome = "Hamburguer", Ativo = true };
            dbContext.TipoItensCardapio.Add(tipo);
            await dbContext.SaveChangesAsync();

            var item = new ItensCardapio { Nome = "Hamburguer X", Preco = 15.00m, Ativo = true, TipoId = tipo.Id };
            dbContext.ItensCardapio.Add(item);
            await dbContext.SaveChangesAsync();
        });

        var client = _factory.CreateClient();
        var request = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 1, Quantidade = 2 }
            }
        };

        var response = await client.PostAsJsonAsync("/api/pedidos", request);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PedidoResponseDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task CreatePedido_WithItemNotInMenu_ShouldReturnNotFoundWithMessage()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            await Task.CompletedTask;
        });

        var client = _factory.CreateClient();
        var request = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 999, Quantidade = 1 }
            }
        };

        var response = await client.PostAsJsonAsync("/api/pedidos", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("item do pedido não encontrado no menu.", content);
    }

    [Fact]
    public async Task CreatePedido_WithQuantityZeroOrLess_ShouldReturnBadRequestWithMessage()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            var tipo = new TipoItensCardapio { Nome = "Hamburguer", Ativo = true };
            dbContext.TipoItensCardapio.Add(tipo);
            await dbContext.SaveChangesAsync();

            var item = new ItensCardapio { Nome = "Hamburguer X", Preco = 15.00m, Ativo = true, TipoId = tipo.Id };
            dbContext.ItensCardapio.Add(item);
            await dbContext.SaveChangesAsync();
        });

        var client = _factory.CreateClient();
        var request = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 1, Quantidade = 0 }
            }
        };

        var response = await client.PostAsJsonAsync("/api/pedidos", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("quantidade dos itens do pedido deve ser maior que zero.", content);
    }

    [Fact]
    public async Task CreatePedido_ShouldCalculateTotalValueFromItemsAndQuantities()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            var tipo = new TipoItensCardapio { Nome = "Hamburguer", Ativo = true };
            dbContext.TipoItensCardapio.Add(tipo);
            await dbContext.SaveChangesAsync();

            var items = new[]
            {
                new ItensCardapio { Nome = "Hamburguer X", Preco = 15.00m, Ativo = true, TipoId = tipo.Id },
                new ItensCardapio { Nome = "Hamburguer G", Preco = 20.00m, Ativo = true, TipoId = tipo.Id }
            };
            dbContext.ItensCardapio.AddRange(items);
            await dbContext.SaveChangesAsync();
        });

        var client = _factory.CreateClient();
        var request = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 1, Quantidade = 2 },
                new ItemPedidoRequestDto { Id = 2, Quantidade = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", request);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var getResponse = await client.GetAsync($"/api/pedidos/{createdPedido.Id}");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        var pedido = JsonSerializer.Deserialize<PedidoGetResponseDto>(getContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(50.00m, pedido.ValorTotal);
    }

    [Fact]
    public async Task CreatePedido_ShouldSavePedidoInRedisAndReturnId()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            var tipo = new TipoItensCardapio { Nome = "Hamburguer", Ativo = true };
            dbContext.TipoItensCardapio.Add(tipo);
            await dbContext.SaveChangesAsync();

            var item = new ItensCardapio { Nome = "Hamburguer X", Preco = 15.00m, Ativo = true, TipoId = tipo.Id };
            dbContext.ItensCardapio.Add(item);
            await dbContext.SaveChangesAsync();
        });

        var client = _factory.CreateClient();
        var request = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 1, Quantidade = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", request);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var getResponse = await client.GetAsync($"/api/pedidos/{createdPedido.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(createdPedido.Id);
    }

    #endregion

    #region PUT /api/pedidos/{id} Tests

    [Fact]
    public async Task UpdatePedido_WithValidItems_ShouldReturnNoContent()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            var tipo = new TipoItensCardapio { Nome = "Hamburguer", Ativo = true };
            dbContext.TipoItensCardapio.Add(tipo);
            await dbContext.SaveChangesAsync();

            var items = new[]
            {
                new ItensCardapio { Nome = "Hamburguer X", Preco = 15.00m, Ativo = true, TipoId = tipo.Id },
                new ItensCardapio { Nome = "Hamburguer G", Preco = 20.00m, Ativo = true, TipoId = tipo.Id }
            };
            dbContext.ItensCardapio.AddRange(items);
            await dbContext.SaveChangesAsync();
        });

        var client = _factory.CreateClient();

        var createRequest = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 1, Quantidade = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var updateRequest = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 2, Quantidade = 2 }
            }
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/pedidos/{createdPedido.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    }

    [Fact]
    public async Task UpdatePedido_WithNonExistentPedido_ShouldReturnNotFoundWithMessage()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            var tipo = new TipoItensCardapio { Nome = "Hamburguer", Ativo = true };
            dbContext.TipoItensCardapio.Add(tipo);
            await dbContext.SaveChangesAsync();

            var item = new ItensCardapio { Nome = "Hamburguer X", Preco = 15.00m, Ativo = true, TipoId = tipo.Id };
            dbContext.ItensCardapio.Add(item);
            await dbContext.SaveChangesAsync();
        });

        var client = _factory.CreateClient();
        var request = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 1, Quantidade = 1 }
            }
        };

        var response = await client.PutAsJsonAsync($"/api/pedidos/{Guid.NewGuid()}", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("pedido não encontrado.", content);
    }

    [Fact]
    public async Task UpdatePedido_WithItemNotInMenu_ShouldReturnNotFoundWithMessage()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            var tipo = new TipoItensCardapio { Nome = "Hamburguer", Ativo = true };
            dbContext.TipoItensCardapio.Add(tipo);
            await dbContext.SaveChangesAsync();

            var item = new ItensCardapio { Nome = "Hamburguer X", Preco = 15.00m, Ativo = true, TipoId = tipo.Id };
            dbContext.ItensCardapio.Add(item);
            await dbContext.SaveChangesAsync();
        });

        var client = _factory.CreateClient();

        var createRequest = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 1, Quantidade = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var updateRequest = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 999, Quantidade = 1 }
            }
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/pedidos/{createdPedido.Id}", updateRequest);
        var updateContent = await updateResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
        Assert.Contains("item do pedido não encontrado no menu.", updateContent);
    }

    [Fact]
    public async Task UpdatePedido_WithQuantityZeroOrLess_ShouldReturnBadRequestWithMessage()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            var tipo = new TipoItensCardapio { Nome = "Hamburguer", Ativo = true };
            dbContext.TipoItensCardapio.Add(tipo);
            await dbContext.SaveChangesAsync();

            var item = new ItensCardapio { Nome = "Hamburguer X", Preco = 15.00m, Ativo = true, TipoId = tipo.Id };
            dbContext.ItensCardapio.Add(item);
            await dbContext.SaveChangesAsync();
        });

        var client = _factory.CreateClient();

        var createRequest = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 1, Quantidade = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var updateRequest = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 1, Quantidade = 0 }
            }
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/pedidos/{createdPedido.Id}", updateRequest);
        var updateContent = await updateResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
        Assert.Contains("quantidade dos itens do pedido deve ser maior que zero.", updateContent);
    }

    [Fact]
    public async Task UpdatePedido_ShouldReplaceItemsInRedisWithNewValues()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            var tipo = new TipoItensCardapio { Nome = "Hamburguer", Ativo = true };
            dbContext.TipoItensCardapio.Add(tipo);
            await dbContext.SaveChangesAsync();

            var items = new[]
            {
                new ItensCardapio { Nome = "Hamburguer X", Preco = 15.00m, Ativo = true, TipoId = tipo.Id },
                new ItensCardapio { Nome = "Hamburguer G", Preco = 20.00m, Ativo = true, TipoId = tipo.Id }
            };
            dbContext.ItensCardapio.AddRange(items);
            await dbContext.SaveChangesAsync();
        });

        var client = _factory.CreateClient();

        var createRequest = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 1, Quantidade = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var updateRequest = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 2, Quantidade = 3 }
            }
        };

        await client.PutAsJsonAsync($"/api/pedidos/{createdPedido.Id}", updateRequest);

        var getResponse = await client.GetAsync($"/api/pedidos/{createdPedido.Id}");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        var pedido = JsonSerializer.Deserialize<PedidoGetResponseDto>(getContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Single(pedido.Itens);
        Assert.Equal(2, pedido.Itens[0].Id);
        Assert.Equal(3, pedido.Itens[0].Quantidade);
        Assert.Equal(60.00m, pedido.ValorTotal);
    }

    #endregion
}
