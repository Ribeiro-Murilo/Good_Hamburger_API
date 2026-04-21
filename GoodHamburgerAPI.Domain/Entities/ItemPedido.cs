namespace GoodHamburgerAPI.Domain.Entities;

public class ItemPedido
{
    public int Id { get; set; }
    public Guid PedidoId { get; set; }
    public int Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Valor { get; set; }

    public Pedido? Pedido { get; set; }
}
