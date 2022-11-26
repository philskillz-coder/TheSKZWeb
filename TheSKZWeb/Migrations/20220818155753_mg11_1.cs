using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheSKZWeb.Migrations
{
    public partial class mg11_1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_Permissions_ChildOfId",
                table: "Permissions");

            migrationBuilder.RenameColumn(
                name: "ChildOfId",
                table: "Permissions",
                newName: "ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_Permissions_ChildOfId",
                table: "Permissions",
                newName: "IX_Permissions_ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_Permissions_ParentId",
                table: "Permissions",
                column: "ParentId",
                principalTable: "Permissions",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_Permissions_ParentId",
                table: "Permissions");

            migrationBuilder.RenameColumn(
                name: "ParentId",
                table: "Permissions",
                newName: "ChildOfId");

            migrationBuilder.RenameIndex(
                name: "IX_Permissions_ParentId",
                table: "Permissions",
                newName: "IX_Permissions_ChildOfId");

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_Permissions_ChildOfId",
                table: "Permissions",
                column: "ChildOfId",
                principalTable: "Permissions",
                principalColumn: "Id");
        }
    }
}
