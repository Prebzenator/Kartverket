using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WebApplication1.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddObstacleCategoryAndFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Obstacles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ObstacleCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObstacleCategories", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "ObstacleCategories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Fallen Tree" },
                    { 2, "Power Line" },
                    { 3, "Construction Crane" },
                    { 4, "Temporary Obstacle" },
                    { 5, "Other" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Obstacles_CategoryId",
                table: "Obstacles",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Obstacles_ObstacleCategories_CategoryId",
                table: "Obstacles",
                column: "CategoryId",
                principalTable: "ObstacleCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Obstacles_ObstacleCategories_CategoryId",
                table: "Obstacles");

            migrationBuilder.DropTable(
                name: "ObstacleCategories");

            migrationBuilder.DropIndex(
                name: "IX_Obstacles_CategoryId",
                table: "Obstacles");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Obstacles");
        }
    }
}
