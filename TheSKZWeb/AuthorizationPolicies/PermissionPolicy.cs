using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TheSKZWeb.Models;
using TheSKZWeb.Services;
using System.Collections.Generic;
using TheSKZWeb.Areas.Users.Models.Administrator.Out;

namespace TheSKZWeb.AuthorizationPolicies
{
    public class Roles
    {
        public static List<string> Administrator = new List<string>()
        {
             "Administrator",
             "Administrator.Administrator",
             "Permissions",
             "Permissions.Administrator",
             "Permissions.Administrator.Create",
             "Permissions.Administrator.Permission",
             "Permissions.Administrator.Permission.Default",
             "Permissions.Administrator.Permission.Delete",
             "Permissions.Administrator.Permission.Name",
             "Roles",
             "Roles.Administrator",
             "Roles.Administrator.Create",
             "Roles.Administrator.Role",
             "Roles.Administrator.Role.Default",
             "Roles.Administrator.Role.Delete",
             "Roles.Administrator.Role.Name",
             "Roles.Administrator.Role.Permissions",
             "Roles.Administrator.Role.Permissions.Grant",
             "Roles.Administrator.Role.Permissions.Revoke",
             "Users",
             "Users.Administrator",
             "Users.Administrator.Create",
             "Users.Administrator.User",
             "Users.Administrator.User.Delete",
             "Users.Administrator.User.Name",
             "Users.Administrator.User.Permissions",
             "Users.Administrator.User.Permissions.Grant",
             "Users.Administrator.User.Permissions.Revoke",
             "Users.Administrator.User.Roles",
             "Users.Administrator.User.Roles.Grant",
             "Users.Administrator.User.Roles.Revoke",
             "Users.Home",
             "Users.Home.Login",
             "Users.Home.Me",
             "ShortLinks",
             "ShortLinks.Administrator",
             "ShortLinks.Home",
             "ShortLinks.Home.Create"
        };
    }

    public class PagePermissions
    {
        public const string Administrator = "jqJPbygW";
        public const string Administrator_Administrator = "wpm0LmdZ";
        public const string Permissions = "bkyDzJdL";
        public const string Permissions_Administrator = "BMlX8lb1";
        public const string Permissions_Administrator_Create = "Qoy2LJAn";
        public const string Permissions_Administrator_Permission = "QRmLGJ6W";
        public const string Permissions_Administrator_Permission_Default = "5YlAzJMx";
        public const string Permissions_Administrator_Permission_Delete = "pxmvOyYV";
        public const string Permissions_Administrator_Permission_Name = "okJ8PJ45";
        public const string Roles = "qxy1xmkv";
        public const string Roles_Administrator = "6Nmg4yKn";
        public const string Roles_Administrator_Create = "WwJqbmVd";
        public const string Roles_Administrator_Role = "oNyNGl12";
        public const string Roles_Administrator_Role_Default = "rMmdVlgp";
        public const string Roles_Administrator_Role_Delete = "AYJnBJng";
        public const string Roles_Administrator_Role_Name = "73ywvmPr";
        public const string Roles_Administrator_Role_Permissions = "oqlKXJAD";
        public const string Roles_Administrator_Role_Permissions_Grant = "0PlrAyLz";
        public const string Roles_Administrator_Role_Permissions_Revoke = "5AJVBJN0";
        public const string Users = "L7JMjmKn";
        public const string Users_Administrator = "NBlGNyZP";
        public const string Users_Administrator_Create = "n1ljeyqk";
        public const string Users_Administrator_User = "qXlaMlvQ";
        public const string Users_Administrator_User_Delete = "V2mb8lvw";
        public const string Users_Administrator_User_Name = "BeJxPJaD";
        public const string Users_Administrator_User_Permissions = "nGyYPmA2";
        public const string Users_Administrator_User_Permissions_Grant = "3ZlEVJ1z";
        public const string Users_Administrator_User_Permissions_Revoke = "YRy7wyzv";
        public const string Users_Administrator_User_Roles = "8ay6Ry9W";
        public const string Users_Administrator_User_Roles_Grant = "P2lR9yVE";
        public const string Users_Administrator_User_Roles_Revoke = "XRJzrJ9r";
        public const string Users_Home = "6Ql4MJrM";
        public const string Users_Home_Login = "aVlo9mO6";
        public const string Users_Home_Me = "bNyQVl6W";
        public const string ShortLinks = "RMl5ZJD9";
        public const string ShortLinks_Administrator = "b5mBWy9A";
        public const string ShortLinks_Home = "WqmeamzR";
        public const string ShortLinks_Home_Create = "j2JOBl1D";
    }


    public class PermissionAttribute : TypeFilterAttribute
    {
        public PermissionAttribute(params string[] requiredPermissions) : base(typeof(PermissionFilter))
        {
            Arguments = new object[] { requiredPermissions };
        }
    }

    public class PermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly IUserService _userService;
        private readonly IPermissionsService _permissionsService;
        private readonly string[] _requiredPermissions;

        public PermissionFilter(
            IUserService userService,
            IPermissionsService permissionsService,
            string[] requiredPermissions
        )
        {
            _userService = userService;
            _permissionsService = permissionsService;
            _requiredPermissions = requiredPermissions;
        }

        async Task IAsyncAuthorizationFilter.OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            return;
            Claim? claim = (context.HttpContext.User.Identity as ClaimsIdentity)?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (claim == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            if (!_userService.GetUserId(claim.Value, out int userId))
            {
                context.Result = new ForbidResult();
                return;
            }

            User? user = await _userService.GetUserById(userId);
            if (user == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            if (!await _userService.HasPermission(user, p => _permissionsService.GetPermissionHashId(p.Id), _requiredPermissions))
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
