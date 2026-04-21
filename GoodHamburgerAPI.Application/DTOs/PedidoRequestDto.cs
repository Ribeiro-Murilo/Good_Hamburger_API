namespace GoodHamburgerAPI.Application.DTOs;

public class PedidoRequestDto
{
    public List<ItemPedidoRequestDto> Itens { get; set; } = [];
}

public class ItemPedidoRequestDto
{
    public int Id { get; set; }
    public int Quantidade { get; set; }
}
