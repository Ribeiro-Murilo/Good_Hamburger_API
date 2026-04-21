using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoodHamburgerAPI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAtivAndDataExclusaoToPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<sbyte>(
                name: "Ativo",
                table: "tb_pedido",
                type: "tinyint",
                nullable: false,
                defaultValue: (sbyte)1);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataExclusao",
                table: "tb_pedido",
                type: "datetime",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "tb_pedido");

            migrationBuilder.DropColumn(
                name: "DataExclusao",
                table: "tb_pedido");
        }
    }
}
