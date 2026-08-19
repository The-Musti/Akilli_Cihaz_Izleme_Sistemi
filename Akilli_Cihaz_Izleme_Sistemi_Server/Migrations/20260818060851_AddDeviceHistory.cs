using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akilli_Cihaz_Izleme_Sistemi_Server.Migrations
{
    public partial class AddDeviceHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHistories_DeviceId_Timestamp",
                table: "DeviceHistories",
                columns: new[] { "DeviceId", "Timestamp" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceHistories");
        }
    }
}
