using GoodHamburgerAPI.Application.DTOs;

namespace GoodHamburgerAPI.Application.Services;

public interface IPedidoService
{
    Task<PedidoResponseDto> CreatePedidoAsync(PedidoRequestDto request);
    Task<PedidoGetResponseDto> GetPedidoAsync(Guid pedidoId);
    Task RemoverItemAsync(Guid pedidoId, int itemId);
    Task FecharPedidoAsync(Guid pedidoId);
    Task DeletarPedidoAsync(Guid pedidoId);
}
