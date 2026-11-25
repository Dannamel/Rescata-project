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
