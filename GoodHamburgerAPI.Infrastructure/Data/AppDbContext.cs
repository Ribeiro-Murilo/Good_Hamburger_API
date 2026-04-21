using GoodHamburgerAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoodHamburgerAPI.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TipoItensCardapio> TipoItensCardapio { get; set; }
    public DbSet<ItensCardapio> ItensCardapio { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureTipoItensCardapio(modelBuilder);
        ConfigureItensCardapio(modelBuilder);
    }

    private void ConfigureTipoItensCardapio(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TipoItensCardapio>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Nome)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnType("varchar(255)");

            entity.Property(e => e.Ativo)
                .HasDefaultValue(true)
                .HasColumnType("tinyint");

            entity.ToTable("tb_tipo_itens_cardapio");
        });
    }

    private void ConfigureItensCardapio(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ItensCardapio>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Nome)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnType("varchar(255)");

            entity.Property(e => e.TipoId)
                .IsRequired()
                .HasColumnType("int");

            entity.Property(e => e.Ativo)
                .HasDefaultValue(true)
                .HasColumnType("tinyint");

            entity.Property(e => e.Preco)
                .IsRequired()
                .HasColumnType("decimal(10,2)");

            entity.HasOne(e => e.Tipo)
                .WithMany(t => t.Itens)
                .HasForeignKey(e => e.TipoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable("tb_itens_cardapio");
        });
    }
}
