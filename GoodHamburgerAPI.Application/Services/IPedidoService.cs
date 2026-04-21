using GoodHamburgerAPI.Application.DTOs;

namespace GoodHamburgerAPI.Application.Services;

public interface IPedidoService
{
    Task<PedidoResponseDto> CreatePedidoAsync(PedidoRequestDto request);
    Task AddItensAsync(Guid pedidoId, PedidoRequestDto request);
    Task<PedidoGetResponseDto> GetPedidoAsync(Guid pedidoId);
}
