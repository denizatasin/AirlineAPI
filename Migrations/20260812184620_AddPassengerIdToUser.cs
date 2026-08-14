using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AirlineAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPassengerIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PassengerId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PassengerId",
                table: "Users",
                column: "PassengerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Passengers_PassengerId",
                table: "Users",
                column: "PassengerId",
                principalTable: "Passengers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Passengers_PassengerId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PassengerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PassengerId",
                table: "Users");
        }
    }
}
