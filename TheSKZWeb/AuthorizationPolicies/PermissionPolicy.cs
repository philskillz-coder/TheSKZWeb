using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TheSKZWeb.Models;
using TheSKZWeb.Services;
using System.Collections.Generic;

namespace TheSKZWeb.AuthorizationPolicies
{
    public class PagePermissions
    {
        // Area Administrator
        public const string Administrator = "DRAMyArQ";

        // Controller Administrator.Administrator
        public const string Administrator_Administrator = "Jqz78El1";
        


        // Area Permissions
        public const string Permissions = "7OWQMjWe";

        // Controller Permissions.Administrator
        public const string Permissions_Administrator = "3pAonlnm";
        public const string Permissions_Administrator_Create = "eQlegWmk";
        public const string Permissions_Administrator_Permission = "NpzG2WP6";
        public const string Permissions_Administrator_Permission_Default = "GKz54Weo";
        public const string Permissions_Administrator_Permission_Delete = "69ldBWed";
        public const string Permissions_Administrator_Permission_Name = "beWaxA2k";



        // Area Roles
        public const string Roles = "aElr45Ww";

        // Controller Roles.Administrator
        public const string Roles_Administrator = "o8l25z9v";
        public const string Roles_Administrator_Create = "vrAZ9lo9";
        public const string Roles_Administrator_Role = "b3z60Ap0";
        public const string Roles_Administrator_Role_Default = "o8lJ00Wk";
        public const string Roles_Administrator_Role_Delete = "8GA1NWXv";
        public const string Roles_Administrator_Role_Name = "x9WP0WB4";
        public const string Roles_Administrator_Role_Permissions = "Y4WYOl0x";
        public const string Roles_Administrator_Role_Permissions_Grant = "v5W0oA4k";
        public const string Roles_Administrator_Role_Permissions_Revoke = "aQABMlyj";



        // Area Users
        public const string Users = "9bAVYMlQ";

        // Controller Users.Administrator
        public const string Users_Administrator = "XVAD4ApQ";
        public const string Users_Administrator_Create = "R4zgw5zL";
        public const string Users_Administrator_User = "7ZzL0zGE";
        public const string Users_Administrator_User_Delete = "7OWQjze1";
        public const string Users_Administrator_User_Name = "Jqz7EA1P";
        public const string Users_Administrator_User_Permissions = "aElr5zwM";
        public const string Users_Administrator_User_Permissions_Grant = "9bAVMlQE";
        public const string Users_Administrator_User_Permissions_Revoke = "R4zg5ALB";
        public const string Users_Administrator_User_Roles = "Qgl8mA34";
        public const string Users_Administrator_User_Roles_Grant = "DZWmEWJK";
        public const string Users_Administrator_User_Roles_Revoke = "orAkDW5R";
        
        // Controller Users.Home
        public const string Users_Home = "DZWm3ElJ";
        public const string Users_Home_Login = "orAk7DA5";
        public const string Users_Home_Me = "orAk7DA5";



        // Area ShortLinks
        public const string ShortLinks = "3pAognAn";

        // Controller ShortLinks.Administrator
        public const string ShortLinks_Administrator = "eQle2gAm";

        // Controller ShortLinks.Home
        public const string ShortLinks_Home = "69ldXBle";
        public const string ShortLinks_Home_Create = "7ZzLb0AG";



        // Special
        public const string __NonExistent__ = "";
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
