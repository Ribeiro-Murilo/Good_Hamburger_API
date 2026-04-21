namespace GoodHamburgerAPI.Domain.Entities;

public class Pedido
{
    public Guid Id { get; set; }
    public decimal ValorSemDesconto { get; set; }
    public decimal ValorFinal { get; set; }
    public bool ComDesconto { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }

    public List<ItemPedido> Itens { get; set; } = [];
}
