using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using TheSKZWeb.ActionFilters;
using TheSKZWeb.Models;
using TheSKZWeb.Services;

namespace TheSKZWeb.Overwrites
{
    [LayoutModel]
    public class NewController : Controller
    {
        protected readonly IUserService _userService;

        public NewController(IUserService userService)
        {
            _userService = userService;
        }

        public bool GetRawIdentity(out ClaimsIdentity? claimsIdentity)
        {
            claimsIdentity = HttpContext.User.Identity as ClaimsIdentity;
            return claimsIdentity != null;
        }

        public async Task<User?> GetIdentity()
        {
            if (!GetRawIdentity(out ClaimsIdentity? identity))
            {
                return null;
            }


            IEnumerable<Claim> claims = identity?.Claims ?? new List<Claim>();

            string? userHashId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            User? _user = await _userService.GetUserByPartial(userHashId);
            if (_user == null)
            {
                return null;
            }

            return _user;
        }
    }
}
