using GoodHamburgerAPI.Application.DTOs;

namespace GoodHamburgerAPI.Application.Services;

public interface IPedidoService
{
    Task<PedidoResponseDto> CreatePedidoAsync(PedidoRequestDto request);
}
