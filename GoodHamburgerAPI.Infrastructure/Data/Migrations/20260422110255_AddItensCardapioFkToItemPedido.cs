using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoodHamburgerAPI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddItensCardapioFkToItemPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ItensCardapioId",
                table: "tb_itens_pedido",
                type: "int",
                nullable: true);

            // Preenche ItensCardapioId com o primeiro item ativo do mesmo tipo para registros antigos
            migrationBuilder.Sql(@"
                UPDATE tb_itens_pedido t
                SET t.ItensCardapioId = (
                    SELECT MIN(id) FROM tb_itens_cardapio c
                    WHERE c.TipoId = t.TipoId AND c.Ativo = 1
                )
                WHERE t.ItensCardapioId IS NULL;
            ", suppressTransaction: false);

            migrationBuilder.AlterColumn<int>(
                name: "ItensCardapioId",
                table: "tb_itens_pedido",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_itens_pedido_ItensCardapioId",
                table: "tb_itens_pedido",
                column: "ItensCardapioId");

            migrationBuilder.AddForeignKey(
                name: "FK_tb_itens_pedido_tb_itens_cardapio_ItensCardapioId",
                table: "tb_itens_pedido",
                column: "ItensCardapioId",
                principalTable: "tb_itens_cardapio",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tb_itens_pedido_tb_itens_cardapio_ItensCardapioId",
                table: "tb_itens_pedido");

            migrationBuilder.DropIndex(
                name: "IX_tb_itens_pedido_ItensCardapioId",
                table: "tb_itens_pedido");

            migrationBuilder.DropColumn(
                name: "ItensCardapioId",
                table: "tb_itens_pedido");
        }
    }
}
