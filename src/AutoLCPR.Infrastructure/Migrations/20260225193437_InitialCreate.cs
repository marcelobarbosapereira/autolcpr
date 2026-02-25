using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoLCPR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Produtores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotasFiscais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChaveAcesso = table.Column<string>(type: "TEXT", maxLength: 44, nullable: true),
                    DataEmissao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NumeroDaNota = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ValorNotaFiscal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Origem = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Destino = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TipoNota = table.Column<int>(type: "INTEGER", nullable: false),
                    ProdutorId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasFiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotasFiscais_Produtores_ProdutorId",
                        column: x => x.ProdutorId,
                        principalTable: "Produtores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rebanhos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IdRebanho = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NomeRebanho = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Mortes = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Nascimentos = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Entradas = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Saidas = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    ProdutorId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rebanhos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rebanhos_Produtores_ProdutorId",
                        column: x => x.ProdutorId,
                        principalTable: "Produtores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_ChaveAcesso",
                table: "NotasFiscais",
                column: "ChaveAcesso",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_ProdutorId",
                table: "NotasFiscais",
                column: "ProdutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Rebanhos_ProdutorId",
                table: "Rebanhos",
                column: "ProdutorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotasFiscais");

            migrationBuilder.DropTable(
                name: "Rebanhos");

            migrationBuilder.DropTable(
                name: "Produtores");
        }
    }
}
