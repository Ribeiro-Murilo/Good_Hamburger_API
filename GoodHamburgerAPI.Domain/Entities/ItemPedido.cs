namespace GoodHamburgerAPI.Domain.Entities;

public class ItemPedido
{
    public int Id { get; set; }
    public Guid PedidoId { get; set; }
    public int TipoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Valor { get; set; }

    public Pedido? Pedido { get; set; }
}
