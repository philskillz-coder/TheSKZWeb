using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheSKZWeb.AuthorizationPolicies;
using TheSKZWeb.Overwrites;
using TheSKZWeb.Services;

namespace TheSKZWeb.Areas.ShortLinks.Controllers
{
    [Area("ShortLinks")]
    [Permission(
        PagePermissions.ShortLinks,
        PagePermissions.ShortLinks_Home
    )]
    public class HomeController : NewController
    {
        public HomeController(IUserService userService) : base(userService)
        {
        }

        [AllowAnonymous]
        [HttpGet("/shortlinks")]
        public IActionResult Index()
        {
            return View();
        }

        [Permission(
            "ShortLinks.Create"
        )]
        [HttpGet("/shortlinks/create")]
        public IActionResult Create()
        {
            return View();
        }
    }
}
