using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pickups.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPickupBusinessId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BusinessId",
                table: "Pickups",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Pickups_BusinessId_ScheduledAt",
                table: "Pickups",
                columns: new[] { "BusinessId", "ScheduledAt" });

            // Las recogidas creadas antes de esta migración quedan con BusinessId vacío y el negocio no las vería.
            // Se recupera el dueño desde la tabla Donations de RescataDonationsDb (misma instancia de SQL Server).
            // Si esa base no existe (por ejemplo, en otra máquina sin Donations), la migración no falla:
            // solo agrega la columna. El UPDATE va en EXEC porque SQL Server no compila una consulta
            // que nombra una base inexistente.
            migrationBuilder.Sql(
                """
                IF DB_ID(N'RescataDonationsDb') IS NOT NULL
                   AND OBJECT_ID(N'RescataDonationsDb.dbo.Donations', N'U') IS NOT NULL
                BEGIN
                    EXEC(N'UPDATE p
                           SET p.BusinessId = d.BusinessId
                           FROM dbo.Pickups AS p
                           INNER JOIN RescataDonationsDb.dbo.Donations AS d ON d.Id = p.DonationId
                           WHERE p.BusinessId = ''00000000-0000-0000-0000-000000000000'';');
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pickups_BusinessId_ScheduledAt",
                table: "Pickups");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "Pickups");
        }
    }
}
