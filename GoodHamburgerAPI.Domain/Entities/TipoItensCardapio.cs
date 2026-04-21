namespace GoodHamburgerAPI.Domain.Entities;

public class TipoItensCardapio
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public bool Ativo { get; set; } = true;

    public ICollection<ItensCardapio> Itens { get; set; } = new List<ItensCardapio>();
}
