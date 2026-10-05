using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegraDeCasamento.Infrastructure.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class DividasEMercado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DividaId",
                table: "Despesas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumeroDaParcela",
                table: "Despesas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Compras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamiliaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dia = table.Column<DateOnly>(type: "date", nullable: false),
                    Local = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CompradoPorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Finalidade = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ParaQuemId = table.Column<Guid>(type: "uuid", nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compras_Familias_FamiliaId",
                        column: x => x.FamiliaId,
                        principalTable: "Familias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dividas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamiliaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    NumeroDeParcelas = table.Column<int>(type: "integer", nullable: false),
                    PrimeiroVencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    DonoId = table.Column<Guid>(type: "uuid", nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dividas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dividas_Familias_FamiliaId",
                        column: x => x.FamiliaId,
                        principalTable: "Familias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItensDaCompra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    CompraId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensDaCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensDaCompra_Compras_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Despesas_DividaId",
                table: "Despesas",
                column: "DividaId");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_FamiliaId_Dia",
                table: "Compras",
                columns: new[] { "FamiliaId", "Dia" });

            migrationBuilder.CreateIndex(
                name: "IX_Dividas_FamiliaId",
                table: "Dividas",
                column: "FamiliaId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensDaCompra_CompraId",
                table: "ItensDaCompra",
                column: "CompraId");

            migrationBuilder.AddForeignKey(
                name: "FK_Despesas_Dividas_DividaId",
                table: "Despesas",
                column: "DividaId",
                principalTable: "Dividas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Despesas_Dividas_DividaId",
                table: "Despesas");

            migrationBuilder.DropTable(
                name: "Dividas");

            migrationBuilder.DropTable(
                name: "ItensDaCompra");

            migrationBuilder.DropTable(
                name: "Compras");

            migrationBuilder.DropIndex(
                name: "IX_Despesas_DividaId",
                table: "Despesas");

            migrationBuilder.DropColumn(
                name: "DividaId",
                table: "Despesas");

            migrationBuilder.DropColumn(
                name: "NumeroDaParcela",
                table: "Despesas");
        }
    }
}
