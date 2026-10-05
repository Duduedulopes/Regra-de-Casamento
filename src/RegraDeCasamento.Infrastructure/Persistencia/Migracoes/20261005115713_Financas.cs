using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegraDeCasamento.Infrastructure.Persistencia.Migracoes
{
    /// <inheritdoc />
    public partial class Financas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Despesas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamiliaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    DonoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Situacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PagaPorId = table.Column<Guid>(type: "uuid", nullable: true),
                    PagaEm = table.Column<DateOnly>(type: "date", nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Despesas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Despesas_Familias_FamiliaId",
                        column: x => x.FamiliaId,
                        principalTable: "Familias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rendas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamiliaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    DonoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DiaDeRecebimento = table.Column<int>(type: "integer", nullable: false),
                    EmDiaUtil = table.Column<bool>(type: "boolean", nullable: false),
                    DataDeFim = table.Column<DateOnly>(type: "date", nullable: true),
                    UsoRestrito = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rendas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rendas_Familias_FamiliaId",
                        column: x => x.FamiliaId,
                        principalTable: "Familias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Despesas_FamiliaId_PagaEm",
                table: "Despesas",
                columns: new[] { "FamiliaId", "PagaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Despesas_FamiliaId_Vencimento",
                table: "Despesas",
                columns: new[] { "FamiliaId", "Vencimento" });

            migrationBuilder.CreateIndex(
                name: "IX_Rendas_FamiliaId",
                table: "Rendas",
                column: "FamiliaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Despesas");

            migrationBuilder.DropTable(
                name: "Rendas");
        }
    }
}
