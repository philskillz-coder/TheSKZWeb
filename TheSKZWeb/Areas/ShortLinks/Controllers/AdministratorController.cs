using Microsoft.AspNetCore.Mvc;
using TheSKZWeb.AuthorizationPolicies;
using TheSKZWeb.Overwrites;
using TheSKZWeb.Services;

namespace TheSKZWeb.Areas.ShortLinks.Controllers
{
    [NonController]
    [Area("ShortLinks")]
    [Permission(
        ServicePermissions.ShortLinks,
        ServicePermissions.ShortLinks_Administrator
    )]
    public class AdministratorController : NewController
    {
        public AdministratorController(IUserService userService) : base(userService)
        {
        }

        [HttpGet("/administrator/shortlinks")]
        public IActionResult ManageShortLinks()
        {
            return View();
        }
    }
}
