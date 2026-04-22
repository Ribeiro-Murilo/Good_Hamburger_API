using System.ComponentModel.DataAnnotations;

namespace GoodHamburgerAPI.Domain.Entities;

public class Desconto
{
    public int Id { get; set; }

    [Range(0, 100)]
    public required int DescontoPorCento { get; set; }

    public bool Ativo { get; set; } = true;
}
