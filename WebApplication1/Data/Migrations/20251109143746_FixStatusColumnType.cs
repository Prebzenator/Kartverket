using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Data.Migrations
{
    public partial class FixStatusColumnType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert existing string values to integers
            migrationBuilder.Sql(@"
                UPDATE Obstacles 
                SET Status = CASE 
                    WHEN Status = 'Pending' THEN 0
                    WHEN Status = 'Approved' THEN 1
                    WHEN Status = 'NotApproved' THEN 2
                    ELSE 0
                END
            ");

            // Change column type from varchar to int
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Obstacles",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert back to string
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Obstacles",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}