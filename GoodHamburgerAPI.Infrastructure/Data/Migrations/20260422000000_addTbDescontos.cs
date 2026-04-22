using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoodHamburgerAPI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class addTbDescontos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_descontos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", "IdentityColumn"),
                    DescontoPorCento = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "tinyint", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_descontos", x => x.Id);
                    table.CheckConstraint("CK_tb_descontos_DescontoPorCento", "DescontoPorCento >= 0 AND DescontoPorCento <= 100");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_descontos");
        }
    }
}
