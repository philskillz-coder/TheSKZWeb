using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using TheSKZWeb.AuthorizationPolicies;
using TheSKZWeb.Models;
using TheSKZWeb.Services;

namespace TheSKZWeb.Middleware
{
    public class LoginManagerMiddleware
    {
        private readonly RequestDelegate _next;

        public LoginManagerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IUserService userService, IPermissionsService permissionsService)
        {
            // skz!
            //User? user = await GetIdentity(context, userService);
            //if (user != null && !await userService.HasPermission(user, p => permissionsService.GetPermissionHashId(p.Id), ServicePermissions.Users_Home_Login)) {
            //    await context.SignOutAsync();
            //}

            // Call the next delegate/middleware in the pipeline.
            await _next(context);
        }

        private bool GetRawIdentity(HttpContext context, out ClaimsIdentity? claimsIdentity)
        {
            claimsIdentity = context.User.Identity as ClaimsIdentity;
            return claimsIdentity != null;
        }

        private async Task<User?> GetIdentity(HttpContext context, IUserService _userService)
        {
            if (!GetRawIdentity(context, out ClaimsIdentity? identity))
            {
                return null;
            }

            IEnumerable<Claim> claims = identity?.Claims ?? new List<Claim>();

            string? userHashId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (userHashId == null)
            {
                return null;
            }


            if (!_userService.GetUserId(userHashId, out int userId))
            {
                return null;
            }

            User? _user = await _userService.GetUserById(userId);
            return _user;
        }
    }

    public static class LoginManagerMiddlewareExtensions
    {
        public static IApplicationBuilder UseLoginManager(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LoginManagerMiddleware>();
        }
    }
}
