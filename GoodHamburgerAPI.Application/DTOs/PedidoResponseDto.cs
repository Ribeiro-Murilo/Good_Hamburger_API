namespace GoodHamburgerAPI.Application.DTOs;

public class PedidoResponseDto
{
    public Guid Id { get; set; }
}

public class PedidoGetResponseDto
{
    public Guid Id { get; set; }
    public List<ItemPedidoResponseDto> Itens { get; set; } = [];
    public bool ComDesconto { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal ValorFinal { get; set; }
    public bool ValorComDesconto { get; set; }
}

public class ItemPedidoResponseDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int Quantidade { get; set; }
}
