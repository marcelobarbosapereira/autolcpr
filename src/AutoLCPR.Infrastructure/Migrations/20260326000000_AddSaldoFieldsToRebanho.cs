using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoLCPR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaldoFieldsToRebanho : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SaldoInicial",
                table: "Rebanhos",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SaldoFinal",
                table: "Rebanhos",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SaldoInicial",
                table: "Rebanhos");

            migrationBuilder.DropColumn(
                name: "SaldoFinal",
                table: "Rebanhos");
        }
    }
}
