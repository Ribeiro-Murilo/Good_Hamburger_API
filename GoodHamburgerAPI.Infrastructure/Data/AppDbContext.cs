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
    public DbSet<Pedido> Pedido { get; set; }
    public DbSet<ItemPedido> ItemPedido { get; set; }
    public DbSet<Desconto> Desconto { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureTipoItensCardapio(modelBuilder);
        ConfigureItensCardapio(modelBuilder);
        ConfigurePedido(modelBuilder);
        ConfigureItemPedido(modelBuilder);
        ConfigureDesconto(modelBuilder);
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

    private void ConfigurePedido(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnType("char(36)");

            entity.Property(e => e.ValorSemDesconto)
                .IsRequired()
                .HasColumnType("decimal(10,2)");

            entity.Property(e => e.ValorFinal)
                .IsRequired()
                .HasColumnType("decimal(10,2)");

            entity.Property(e => e.ComDesconto)
                .IsRequired()
                .HasColumnType("tinyint");

            entity.Property(e => e.CriadoEm)
                .IsRequired()
                .HasColumnType("datetime");

            entity.Property(e => e.AtualizadoEm)
                .IsRequired()
                .HasColumnType("datetime");

            entity.Property(e => e.Ativo)
                .IsRequired()
                .HasDefaultValue(true)
                .HasColumnType("tinyint");

            entity.Property(e => e.DataExclusao)
                .HasColumnType("datetime");

            entity.HasMany(e => e.Itens)
                .WithOne(i => i.Pedido)
                .HasForeignKey(i => i.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable("tb_pedido");
        });
    }

    private void ConfigureItemPedido(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ItemPedido>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PedidoId)
                .IsRequired()
                .HasColumnType("char(36)");

            entity.Property(e => e.TipoId)
                .IsRequired()
                .HasColumnType("int");

            entity.Property(e => e.Nome)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnType("varchar(255)");

            entity.Property(e => e.Valor)
                .IsRequired()
                .HasColumnType("decimal(10,2)");

            entity.HasOne(e => e.Pedido)
                .WithMany(p => p.Itens)
                .HasForeignKey(e => e.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable("tb_itens_pedido");
        });
    }

    private void ConfigureDesconto(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Desconto>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.DescontoPorCento)
                .IsRequired()
                .HasColumnType("int");

            entity.Property(e => e.Ativo)
                .HasDefaultValue(true)
                .HasColumnType("tinyint");

            entity.ToTable("tb_descontos");
        });
    }
}
