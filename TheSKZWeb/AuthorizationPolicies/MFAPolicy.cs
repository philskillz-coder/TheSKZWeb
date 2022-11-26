using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using TheSKZWeb.Areas.Permissions.Models.Administrator.In;
using TheSKZWeb.Services;

namespace TheSKZWeb.AuthorizationPolicies
{
    public class MFAAttribute : TypeFilterAttribute
    {
        public MFAAttribute(string Name) : base(typeof(MFAFilter))
        {
            Arguments = new object[] { Name = Name };
        }

        public class MFAActionFilter : IAsyncActionFilter
        {
            private readonly IMFAService _mfaService;
            private readonly string _name;

            public MFAActionFilter(IMFAService mfaService, string Name)
            {
                _mfaService = mfaService;
                _name = Name;
            }

            public async Task OnActionExecutionAsync(
                ActionExecutingContext context, ActionExecutionDelegate next)
            {
                string? userHashId = (context.HttpContext.User.Identity as ClaimsIdentity)?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (userHashId == null)
                {
                    context.Result = new ForbidResult();
                    return;
                }

                context.ActionArguments.TryGetValue("mfaCode", out object? _mfaCode);
                string? mfaCode = _mfaCode?.ToString();

                if (mfaCode == null)
                {
                    context.Result = new OkObjectResult(
                        new
                        {
                            Message = "2FA Validation required",
                            For = _name,
                            Type = "Error",
                            Data = new
                            {
                                UserIdentifier = userHashId
                            },
                            Tags = new string[]
                            {
                                ResponseTags.DO_MFA
                            }
                        }
                    );
                    return;
                }

                if (!_mfaService.ValidateMFARequest(
                    userHashId, mfaCode
                ))
                {
                    context.Result = new OkObjectResult(
                        new 
                        {
                            Message = "2FA Validation Failed!",
                            For = _name,
                            Type = "Error",
                            Data = new
                            {
                                UserIdentifier = userHashId
                            }
                        }
                    );
                    return;
                }

                // Do something before the action executes.
                await next();
                // Do something after the action executes.
            }
        }

        public class MFAFilter : ActionFilterAttribute
        {
            private readonly IMFAService _mfaService;

            public MFAFilter(IMFAService mfaService)
            {
                _mfaService = mfaService;
            }

            public override void OnActionExecuting(ActionExecutingContext actionContext)
            {
                string? userHashId = (actionContext.HttpContext.User.Identity as ClaimsIdentity)?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (userHashId == null)
                {
                    actionContext.Result = new ForbidResult();
                    return;
                }

                actionContext.ActionArguments.TryGetValue("mfaCode", out object? mfaValue);

                string? mfaCode;
                try
                {
                    mfaCode = (string?)mfaValue;
                    if (mfaCode == null)
                    {
                        actionContext.Result = new ForbidResult();
                        
                        return;
                    }
                } catch
                {
                    actionContext.Result = new ForbidResult();
                    return;
                }

                if (!_mfaService.ValidatePin(_mfaService.GenerateSecret(userHashId), mfaCode))
                {
                    actionContext.Result = new ForbidResult();
                    return;
                }
 
            }
        }

    }
}
