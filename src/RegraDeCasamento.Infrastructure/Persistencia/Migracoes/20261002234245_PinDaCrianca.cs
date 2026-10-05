using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegraDeCasamento.Infrastructure.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class PinDaCrianca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TipoDeLogin",
                table: "AspNetUsers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "EmailESenha"); // contas que já existiam são de adultos
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoDeLogin",
                table: "AspNetUsers");
        }
    }
}
