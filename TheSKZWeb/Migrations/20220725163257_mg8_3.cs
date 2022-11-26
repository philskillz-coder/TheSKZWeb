using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheSKZWeb.Migrations
{
    public partial class mg8_3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCritical",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCritical",
                table: "Permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCritical",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "IsCritical",
                table: "Permissions");
        }
    }
}
