using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TheSKZWeb.Models;
using TheSKZWeb.Overwrites;
using TheSKZWeb.Services;

namespace TheSKZWeb.ActionFilters
{
    public class LayoutModelAttribute : TypeFilterAttribute
    {
        public LayoutModelAttribute() : base(typeof(LayoutModelFilter))
        {
            Arguments = new object[] { };
        }

        public class LayoutModelFilter : Attribute, IAsyncActionFilter
        {
            private readonly IUserService _userService;

            public LayoutModelFilter(IUserService userService)
            {
                _userService = userService;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                //  get the view bag

                NewController? controller = context.Controller as NewController;

                if (controller != null)
                {
                    User? user = await controller.GetIdentity();

                    bool isAdministrator = await _userService.HasPermission(
                        user,
                        p => p.Name,
                        "Administrator"
                    );

                    controller.ViewBag.LayoutModel = new LayoutModel
                    {
                        IsAdministrator = isAdministrator
                    };
                }
                // set the viewbag values

                // Do something before the action executes.
                await next();
                // Do something after the action executes.
            }
        }

    }
}
