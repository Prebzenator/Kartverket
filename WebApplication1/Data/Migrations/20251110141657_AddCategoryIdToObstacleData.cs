using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryIdToObstacleData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Obstacles",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "ObstacleCategories",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Mast or Tower");

            migrationBuilder.UpdateData(
                table: "ObstacleCategories",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Cable");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Obstacles",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.UpdateData(
                table: "ObstacleCategories",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Fallen Tree");

            migrationBuilder.UpdateData(
                table: "ObstacleCategories",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Temporary Obstacle");
        }
    }
}
