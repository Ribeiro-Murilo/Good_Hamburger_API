using GoodHamburgerAPI.Application.DTOs;

namespace GoodHamburgerAPI.Application.Services;

public interface ICardapioService
{
    Task<List<MenuItemDto>> GetMenuAsync(int? tipoItemId = null);
    Task<List<TipoItemDto>> GetTiposItensAsync();
}
