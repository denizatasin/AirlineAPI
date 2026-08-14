using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AirlineAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddDestinationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DestinationId",
                table: "FlightSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Destinations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RangeStart = table.Column<int>(type: "int", nullable: false),
                    RangeEnd = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Destinations", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Destinations",
                columns: new[] { "Id", "City", "RangeEnd", "RangeStart" },
                values: new object[,]
                {
                    { 1, "New York", 4, 1 },
                    { 2, "Los Angeles", 10, 7 },
                    { 3, "Washington", 14, 11 },
                    { 4, "Tokyo", 53, 50 },
                    { 5, "Dubai", 763, 760 },
                    { 6, "Berlin", 1725, 1720 },
                    { 7, "Paris", 1827, 1820 },
                    { 8, "Roma", 1865, 1860 },
                    { 9, "Amsterdam", 1955, 1950 },
                    { 10, "Londra", 1987, 1980 },
                    { 11, "Ankara", 2111, 2100 },
                    { 12, "Izmir", 2311, 2300 },
                    { 13, "Antalya", 2413, 2400 },
                    { 14, "Adana", 2455, 2450 },
                    { 15, "Bodrum", 2513, 2500 },
                    { 16, "Trabzon", 2825, 2820 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlightSchedules_DestinationId",
                table: "FlightSchedules",
                column: "DestinationId");

            migrationBuilder.AddForeignKey(
                name: "FK_FlightSchedules_Destinations_DestinationId",
                table: "FlightSchedules",
                column: "DestinationId",
                principalTable: "Destinations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlightSchedules_Destinations_DestinationId",
                table: "FlightSchedules");

            migrationBuilder.DropTable(
                name: "Destinations");

            migrationBuilder.DropIndex(
                name: "IX_FlightSchedules_DestinationId",
                table: "FlightSchedules");

            migrationBuilder.DropColumn(
                name: "DestinationId",
                table: "FlightSchedules");
        }
    }
}
