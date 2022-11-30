using Microsoft.AspNetCore.Mvc;
using TheSKZWeb.AuthorizationPolicies;
using TheSKZWeb.Overwrites;
using TheSKZWeb.Services;

namespace TheSKZWeb.Areas.Administrator.Controllers;

[Area("Administrator")]
[Permission(
    PagePermissions.Administrator,
    PagePermissions.Administrator_Administrator
)]
public class HomeController : NewController
{
    public HomeController(IUserService userService) : base(userService)
    {
    }

    public IActionResult Index()
    {
        return View();
    }
}
