using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pickups.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPickupConcurrencyAndActiveUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Pickups",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "UX_Pickups_ActiveDonation",
                table: "Pickups",
                column: "DonationId",
                unique: true,
                filter: "[Status] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Pickups_ActiveDonation",
                table: "Pickups");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Pickups");
        }
    }
}
