using Microsoft.AspNetCore.Mvc;
using TheSKZWeb.Areas.Socials.Models.Home.In;
using TheSKZWeb.Overwrites;
using TheSKZWeb.Services;

namespace TheSKZWeb.Areas.Socials.Controllers
{
    [Area("Socials")]
    public class HomeController : NewController
    {
        private readonly Dictionary<string, string> _map;
        public HomeController(IUserService userService) : base(userService)
        {
            _map = new Dictionary<string, string>()
            {
                {"instagram", "https://instagram.com/philskillz.coder"},
                {"tiktok", "https://tiktok.com/@.theskz"},
                {"youtube", "https://youtube.com/channel/UCe4EYxvrWGCWW-pRahZtt6g"},
                {"twitch", "https://twitch.tv/philskillz_"},
                {"epicgames", "https://theskz.dev/disabled"},
                {"steam", "https://steamcommunity.com/id/philskillz_/"},
                {"twitter", "https://twitter.com/Philskillz_"},
                {"github", "https://github.com/philskillz-coder"},
                {"discord", "https://discord.gg/APGDCfZbpW"}
            };
        }

        [HttpGet("/socials")]
        public IActionResult Index(
            [FromQuery] In_Index request
        )
        {
            if (_map.TryGetValue(request.t ?? "", out string? value))
            {
                return Redirect(value);
            }
            //return View();
            return NotFound("not found"); // make view
        }

    }
}
