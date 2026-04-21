using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoodHamburgerAPI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class addTbItensCardapio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_tipo_itens_cardapio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", "IdentityColumn"),
                    Nome = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Ativo = table.Column<bool>(type: "tinyint", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_tipo_itens_cardapio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tb_itens_cardapio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", "IdentityColumn"),
                    Nome = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    TipoId = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "tinyint", nullable: false, defaultValue: true),
                    Preco = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_itens_cardapio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tb_itens_cardapio_tb_tipo_itens_cardapio_TipoId",
                        column: x => x.TipoId,
                        principalTable: "tb_tipo_itens_cardapio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tb_itens_cardapio_TipoId",
                table: "tb_itens_cardapio",
                column: "TipoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_itens_cardapio");

            migrationBuilder.DropTable(
                name: "tb_tipo_itens_cardapio");
        }
    }
}
