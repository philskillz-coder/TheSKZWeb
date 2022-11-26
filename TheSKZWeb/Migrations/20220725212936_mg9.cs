using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

// important: this migration was written manually! not with the command, because unique indexes are not available in efc6.0
// i maybe need this in the future: the contents of the mg9.Up and mg9.Down methods were swappped

namespace TheSKZWeb.Migrations
{
    public partial class mg9 : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId_UserId",
                table: "UserRoles",
                columns: new[] { "RoleId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PermissionId_UserId",
                table: "UserPermissions",
                columns: new[] { "PermissionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId_RoleId",
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                table: "Permissions",
                column: "Name",
                unique: true);
        }

        

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserRoles_RoleId_UserId",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_UserPermissions_PermissionId_UserId",
                table: "UserPermissions");

            migrationBuilder.DropIndex(
                name: "IX_Roles_Name",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_PermissionId_RoleId",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Name",
                table: "Permissions");
        }
    }
}
