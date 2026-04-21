namespace GoodHamburgerAPI.Domain.Entities;

public class Pedido
{
    public Guid Id { get; set; }
    public List<ItemPedido> Itens { get; set; } = [];
    public bool ComDesconto { get; set; }
}

public class ItemPedido
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int Quantidade { get; set; }
}
