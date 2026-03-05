using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoLCPR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracaoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChavesNFe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProdutorId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChaveAcesso = table.Column<string>(type: "TEXT", maxLength: 44, nullable: false),
                    DataImportacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProdutorId1 = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChavesNFe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChavesNFe_Produtores_ProdutorId",
                        column: x => x.ProdutorId,
                        principalTable: "Produtores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChavesNFe_Produtores_ProdutorId1",
                        column: x => x.ProdutorId1,
                        principalTable: "Produtores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Configuracoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImagemCabecalhoRelatorios = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CfopsIgnorados = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    NaturezasIgnoradas = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuracoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lancamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    ClienteFornecedor = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Situacao = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Vencimento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProdutorId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lancamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lancamentos_Produtores_ProdutorId",
                        column: x => x.ProdutorId,
                        principalTable: "Produtores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovimentacoesRebanho",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TipoMovimentacao = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Quantidade = table.Column<int>(type: "INTEGER", nullable: false),
                    ProdutorId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentacoesRebanho", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentacoesRebanho_Produtores_ProdutorId",
                        column: x => x.ProdutorId,
                        principalTable: "Produtores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChavesNFe_ChaveAcesso",
                table: "ChavesNFe",
                column: "ChaveAcesso",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChavesNFe_ProdutorId",
                table: "ChavesNFe",
                column: "ProdutorId");

            migrationBuilder.CreateIndex(
                name: "IX_ChavesNFe_ProdutorId1",
                table: "ChavesNFe",
                column: "ProdutorId1");

            migrationBuilder.CreateIndex(
                name: "IX_Lancamentos_ProdutorId",
                table: "Lancamentos",
                column: "ProdutorId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentacoesRebanho_ProdutorId",
                table: "MovimentacoesRebanho",
                column: "ProdutorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChavesNFe");

            migrationBuilder.DropTable(
                name: "Configuracoes");

            migrationBuilder.DropTable(
                name: "Lancamentos");

            migrationBuilder.DropTable(
                name: "MovimentacoesRebanho");
        }
    }
}
