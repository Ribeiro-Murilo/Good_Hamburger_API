using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GoodHamburgerAPI.Application.DTOs;
using GoodHamburgerAPI.Domain.Entities;
using GoodHamburgerAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
                new ItemPedidoRequestDto { Id = 1 }
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
                new ItemPedidoRequestDto { Id = 999 }
            }
        };

        var response = await client.PostAsJsonAsync("/api/pedidos", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("item do pedido não encontrado no menu.", content);
    }

    [Fact]
    public async Task CreatePedido_ShouldCalculateTotalValueFromItems()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            var tipo = new TipoItensCardapio { Nome = "Hamburguer", Ativo = true };
            dbContext.TipoItensCardapio.Add(tipo);
            await dbContext.SaveChangesAsync();

            var tipo2 = new TipoItensCardapio { Nome = "Bebida", Ativo = true };
            dbContext.TipoItensCardapio.Add(tipo2);
            await dbContext.SaveChangesAsync();

            var items = new[]
            {
                new ItensCardapio { Nome = "Hamburguer X", Preco = 15.00m, Ativo = true, TipoId = tipo.Id },
                new ItensCardapio { Nome = "Refrigerante", Preco = 5.00m, Ativo = true, TipoId = tipo2.Id }
            };
            dbContext.ItensCardapio.AddRange(items);
            await dbContext.SaveChangesAsync();
        });

        var client = _factory.CreateClient();
        var request = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 1 },
                new ItemPedidoRequestDto { Id = 2 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", request);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var getResponse = await client.GetAsync($"/api/pedidos/{createdPedido.Id}");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        var pedido = JsonSerializer.Deserialize<PedidoGetResponseDto>(getContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(20.00m, pedido.ValorTotal);
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
                new ItemPedidoRequestDto { Id = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", request);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var getResponse = await client.GetAsync($"/api/pedidos/{createdPedido.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(createdPedido.Id);
    }

    [Fact]
    public async Task CreatePedido_WithDuplicateItem_ShouldReturnBadRequest()
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
                new ItemPedidoRequestDto { Id = 1 },
                new ItemPedidoRequestDto { Id = 1 }
            }
        };

        var response = await client.PostAsJsonAsync("/api/pedidos", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("não é permitido adicionar o mesmo item mais de uma vez no pedido.", content);
    }

    [Fact]
    public async Task CreatePedido_WithDuplicateCategory_ShouldReturnBadRequest()
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
                new ItemPedidoRequestDto { Id = 1 },
                new ItemPedidoRequestDto { Id = 2 }
            }
        };

        var response = await client.PostAsJsonAsync("/api/pedidos", request);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("não é permitido mais de um item da mesma categoria no pedido.", content);
    }

    #endregion

    #region GET /api/pedidos/{id} Tests

    [Fact]
    public async Task GetPedido_WithPedidoInDatabase_ShouldReturnPedidoFromDatabase()
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
                new ItemPedidoRequestDto { Id = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var closeResponse = await client.PostAsync($"/api/pedidos/{createdPedido!.Id}/fechar", null);
        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/pedidos/{createdPedido.Id}");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        var pedido = JsonSerializer.Deserialize<PedidoGetResponseDto>(getContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(pedido);
        Assert.Equal(createdPedido.Id, pedido.Id);
        Assert.Single(pedido.Itens);
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

            var tipo2 = new TipoItensCardapio { Nome = "Bebida", Ativo = true };
            dbContext.TipoItensCardapio.Add(tipo2);
            await dbContext.SaveChangesAsync();

            var items = new[]
            {
                new ItensCardapio { Nome = "Hamburguer X", Preco = 15.00m, Ativo = true, TipoId = tipo.Id },
                new ItensCardapio { Nome = "Refrigerante", Preco = 5.00m, Ativo = true, TipoId = tipo2.Id }
            };
            dbContext.ItensCardapio.AddRange(items);
            await dbContext.SaveChangesAsync();
        });

        var client = _factory.CreateClient();

        var createRequest = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var updateRequest = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 2 }
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
                new ItemPedidoRequestDto { Id = 1 }
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
                new ItemPedidoRequestDto { Id = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var updateRequest = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 999 }
            }
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/pedidos/{createdPedido.Id}", updateRequest);
        var updateContent = await updateResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
        Assert.Contains("item do pedido não encontrado no menu.", updateContent);
    }

    [Fact]
    public async Task UpdatePedido_WithPedidoAlreadyInDatabase_ShouldReturnBadRequestWithMessage()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            var tipo = new TipoItensCardapio { Nome = "Hamburguer", Ativo = true };
            dbContext.TipoItensCardapio.Add(tipo);
            await dbContext.SaveChangesAsync();

            var tipo2 = new TipoItensCardapio { Nome = "Bebida", Ativo = true };
            dbContext.TipoItensCardapio.Add(tipo2);
            await dbContext.SaveChangesAsync();

            var items = new[]
            {
                new ItensCardapio { Nome = "Hamburguer X", Preco = 15.00m, Ativo = true, TipoId = tipo.Id },
                new ItensCardapio { Nome = "Refrigerante", Preco = 5.00m, Ativo = true, TipoId = tipo2.Id }
            };
            dbContext.ItensCardapio.AddRange(items);
            await dbContext.SaveChangesAsync();
        });

        var client = _factory.CreateClient();

        var createRequest = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var closeResponse = await client.PostAsync($"/api/pedidos/{createdPedido!.Id}/fechar", null);
        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);

        var updateRequest = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 2 }
            }
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/pedidos/{createdPedido.Id}", updateRequest);
        var updateContent = await updateResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
        Assert.Contains("pedido já foi finalizado, não é possível fazer mais alterações.", updateContent);
    }

    [Fact]
    public async Task UpdatePedido_ShouldReplaceItemsInRedisWithNewValues()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            var tipo = new TipoItensCardapio { Nome = "Hamburguer", Ativo = true };
            dbContext.TipoItensCardapio.Add(tipo);
            await dbContext.SaveChangesAsync();

            var tipo2 = new TipoItensCardapio { Nome = "Bebida", Ativo = true };
            dbContext.TipoItensCardapio.Add(tipo2);
            await dbContext.SaveChangesAsync();

            var items = new[]
            {
                new ItensCardapio { Nome = "Hamburguer X", Preco = 15.00m, Ativo = true, TipoId = tipo.Id },
                new ItensCardapio { Nome = "Refrigerante", Preco = 5.00m, Ativo = true, TipoId = tipo2.Id }
            };
            dbContext.ItensCardapio.AddRange(items);
            await dbContext.SaveChangesAsync();
        });

        var client = _factory.CreateClient();

        var createRequest = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var updateRequest = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 2 }
            }
        };

        await client.PutAsJsonAsync($"/api/pedidos/{createdPedido.Id}", updateRequest);

        var getResponse = await client.GetAsync($"/api/pedidos/{createdPedido.Id}");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        var pedido = JsonSerializer.Deserialize<PedidoGetResponseDto>(getContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Single(pedido.Itens);
        Assert.Equal(2, pedido.Itens[0].Id);
        Assert.Equal(5.00m, pedido.ValorTotal);
    }

    [Fact]
    public async Task UpdatePedido_WithDuplicateItem_ShouldReturnBadRequest()
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
                new ItemPedidoRequestDto { Id = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var updateRequest = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 1 },
                new ItemPedidoRequestDto { Id = 1 }
            }
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/pedidos/{createdPedido.Id}", updateRequest);
        var updateContent = await updateResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
        Assert.Contains("não é permitido adicionar o mesmo item mais de uma vez no pedido.", updateContent);
    }

    [Fact]
    public async Task UpdatePedido_WithDuplicateCategory_ShouldReturnBadRequest()
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
                new ItemPedidoRequestDto { Id = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var updateRequest = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 1 },
                new ItemPedidoRequestDto { Id = 2 }
            }
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/pedidos/{createdPedido.Id}", updateRequest);
        var updateContent = await updateResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
        Assert.Contains("não é permitido mais de um item da mesma categoria no pedido.", updateContent);
    }

    #endregion

    #region POST /api/pedidos/{id}/fechar Tests

    [Fact]
    public async Task FecharPedido_WithExistingPedido_ShouldReturnOk()
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
                new ItemPedidoRequestDto { Id = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var response = await client.PostAsync($"/api/pedidos/{createdPedido!.Id}/fechar", new StringContent("", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task FecharPedido_WithNonExistentPedido_ShouldReturnNotFoundWithMessage()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            await Task.CompletedTask;
        });

        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/pedidos/{Guid.NewGuid()}/fechar", null);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("pedido não encontrado.", content);
    }

    [Fact]
    public async Task FecharPedido_ShouldPersistPedidoInDatabase()
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
                new ItemPedidoRequestDto { Id = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        await client.PostAsync($"/api/pedidos/{createdPedido!.Id}/fechar", null);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pedidoNoBanco = await dbContext.Pedido
            .Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.Id == createdPedido.Id);

        Assert.NotNull(pedidoNoBanco);
        Assert.Single(pedidoNoBanco.Itens);
        Assert.Equal(15.00m, pedidoNoBanco.Itens[0].Valor);
    }

    [Fact]
    public async Task FecharPedido_ShouldRemovePedidoFromRedis()
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
                new ItemPedidoRequestDto { Id = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var fecharResponse = await client.PostAsync($"/api/pedidos/{createdPedido!.Id}/fechar", null);
        Assert.Equal(HttpStatusCode.OK, fecharResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/pedidos/{createdPedido.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var addItensRequest = new PedidoRequestDto
        {
            Itens = new List<ItemPedidoRequestDto>
            {
                new ItemPedidoRequestDto { Id = 1 }
            }
        };

        var addResponse = await client.PutAsJsonAsync($"/api/pedidos/{createdPedido.Id}", addItensRequest);
        Assert.Equal(HttpStatusCode.BadRequest, addResponse.StatusCode);
    }

    #endregion

    #region DELETE /api/pedidos/{id} Tests

    [Fact]
    public async Task DeletarPedido_WithPedidoInRedis_ShouldReturnOkAndRemoveFromRedis()
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
                new ItemPedidoRequestDto { Id = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var deleteResponse = await client.DeleteAsync($"/api/pedidos/{createdPedido!.Id}");

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/pedidos/{createdPedido.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeletarPedido_WithPedidoInDatabase_ShouldReturnOkAndInactivatePedido()
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
                new ItemPedidoRequestDto { Id = 1 }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/pedidos", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPedido = JsonSerializer.Deserialize<PedidoResponseDto>(createContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        await client.PostAsync($"/api/pedidos/{createdPedido!.Id}/fechar", null);

        var deleteResponse = await client.DeleteAsync($"/api/pedidos/{createdPedido.Id}");

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pedidoNoBanco = await dbContext.Pedido.FirstOrDefaultAsync(p => p.Id == createdPedido.Id);

        Assert.NotNull(pedidoNoBanco);
        Assert.False(pedidoNoBanco.Ativo);
        Assert.NotNull(pedidoNoBanco.DataExclusao);
    }

    [Fact]
    public async Task DeletarPedido_WithNonExistentPedido_ShouldReturnNotFoundWithMessage()
    {
        await ClearAndSeedDatabaseAsync(async dbContext =>
        {
            await Task.CompletedTask;
        });

        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"/api/pedidos/{Guid.NewGuid()}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("pedido não encontrado.", content);
    }

    #endregion
}
