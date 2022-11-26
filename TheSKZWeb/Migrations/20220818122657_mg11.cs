using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheSKZWeb.Migrations
{
    public partial class mg11 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChildOfId",
                table: "Permissions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_ChildOfId",
                table: "Permissions",
                column: "ChildOfId");

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_Permissions_ChildOfId",
                table: "Permissions",
                column: "ChildOfId",
                principalTable: "Permissions",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_Permissions_ChildOfId",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_ChildOfId",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "ChildOfId",
                table: "Permissions");
        }
    }
}
