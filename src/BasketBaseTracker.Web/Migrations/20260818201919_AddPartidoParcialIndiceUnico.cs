using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BasketBaseTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPartidoParcialIndiceUnico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PartidosParciales_PartidoId",
                table: "PartidosParciales");

            migrationBuilder.CreateIndex(
                name: "IX_PartidosParciales_PartidoId_NumeroPeriodo",
                table: "PartidosParciales",
                columns: new[] { "PartidoId", "NumeroPeriodo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PartidosParciales_PartidoId_NumeroPeriodo",
                table: "PartidosParciales");

            migrationBuilder.CreateIndex(
                name: "IX_PartidosParciales_PartidoId",
                table: "PartidosParciales",
                column: "PartidoId");
        }
    }
}
