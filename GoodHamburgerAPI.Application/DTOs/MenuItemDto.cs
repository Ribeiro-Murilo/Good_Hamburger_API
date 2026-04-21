namespace GoodHamburgerAPI.Application.DTOs;

public class MenuItemDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public TipoItemDto Tipo { get; set; } = new();
}
