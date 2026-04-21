using GoodHamburgerAPI.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GoodHamburgerAPI.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IHealthCheckService, HealthCheckService>();
        services.AddScoped<ICardapioService, CardapioService>();
        services.AddScoped<IPedidoService, PedidoService>();

        return services;
    }
}
