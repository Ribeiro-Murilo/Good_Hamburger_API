using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoodHamburgerAPI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveQuantidadeAddTipoIdInItemPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValorTotal",
                table: "tb_itens_pedido");

            migrationBuilder.DropColumn(
                name: "ValorUnitario",
                table: "tb_itens_pedido");

            migrationBuilder.RenameColumn(
                name: "Quantidade",
                table: "tb_itens_pedido",
                newName: "TipoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TipoId",
                table: "tb_itens_pedido",
                newName: "Quantidade");

            migrationBuilder.AddColumn<decimal>(
                name: "ValorTotal",
                table: "tb_itens_pedido",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorUnitario",
                table: "tb_itens_pedido",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
