namespace GoodHamburgerAPI.Domain.Entities;

public class ItensCardapio
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public int TipoId { get; set; }
    public bool Ativo { get; set; } = true;
    public decimal Preco { get; set; }

    public TipoItensCardapio? Tipo { get; set; }
}
