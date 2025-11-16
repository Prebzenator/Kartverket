using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Data.Migrations
{
    public partial class MakeCategoryIdNullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET @fk := (
  SELECT CONSTRAINT_NAME
  FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
  WHERE TABLE_SCHEMA = 'kartverket'
    AND TABLE_NAME = 'Obstacles'
    AND COLUMN_NAME = 'CategoryId'
    AND REFERENCED_TABLE_NAME = 'ObstacleCategories'
  LIMIT 1
);
SET @sql := IF(@fk IS NOT NULL, CONCAT('ALTER TABLE `Obstacles` DROP FOREIGN KEY `', @fk, '`'), 'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
");


            migrationBuilder.AlterColumn<decimal>(
                name: "ObstacleHeight",
                table: "Obstacles",
                type: "decimal(65,30)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Obstacles",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Obstacles_ObstacleCategories_CategoryId",
                table: "Obstacles",
                column: "CategoryId",
                principalTable: "ObstacleCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET @fk := (
  SELECT CONSTRAINT_NAME
  FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
  WHERE TABLE_SCHEMA = 'kartverket'
    AND TABLE_NAME = 'Obstacles'
    AND COLUMN_NAME = 'CategoryId'
    AND REFERENCED_TABLE_NAME = 'ObstacleCategories'
  LIMIT 1
);
SET @sql := IF(@fk IS NOT NULL, CONCAT('ALTER TABLE `Obstacles` DROP FOREIGN KEY `', @fk, '`'), 'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
");


            migrationBuilder.AlterColumn<decimal>(
                name: "ObstacleHeight",
                table: "Obstacles",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Obstacles",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Obstacles_ObstacleCategories_CategoryId",
                table: "Obstacles",
                column: "CategoryId",
                principalTable: "ObstacleCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
