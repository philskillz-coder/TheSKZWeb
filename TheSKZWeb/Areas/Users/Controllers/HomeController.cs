using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheSKZWeb.Areas.Users.Models.Home.In;
using TheSKZWeb.Areas.Users.Models.Home.Out;
using TheSKZWeb.AuthorizationPolicies;
using TheSKZWeb.Models;
using TheSKZWeb.Overwrites;
using TheSKZWeb.Services;

namespace TheSKZWeb.Areas.Permissions.Controllers
{
    [Area("Users")]
    [Permission(
        PagePermissions.Users,
        PagePermissions.Users_Home
    )]
    public class HomeController : NewController
    {
        private readonly IRolesService _rolesService;
        private readonly IMFAService _mfaService;
        private readonly IPermissionsService _permissionsService;

        public HomeController(IUserService userService, IRolesService rolesService, IMFAService mfaService, IPermissionsService permissionsService) : base(userService)
        {
            _rolesService = rolesService;
            _mfaService = mfaService;
            _permissionsService = permissionsService;
        }


        [HttpGet("/users/register")]
        [AllowAnonymous]
        public IActionResult Register(

        )
        {
            return View();
        }

        [HttpPost("/users/register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(
            [FromForm] In_Register request
        )
        {
            if (request.Username == null || request.Password == null)
            {
                return Ok(
                    new
                    {
                        Message = "Missing Data!",
                        For = "Register",
                        Type = "Error"
                    }
                );
            }

            if (await _userService.UsernameExists(request.Username))
            {
                return Ok(
                    new
                    {
                        Message = "Username already exists!",
                        For = "Register",
                        Type = "Error",
                        Data = new
                        {
                            Username = request.Username
                        }
                    }
                );
            }

            _userService.CreateRecoveryToken(32, out string recoveryTokenPlain, out byte[] recoveryTokenHash, out byte[] recoveryTokenSalt);
            User user = await _userService.CreateUser(
                request.Username,
                request.Password,
                recoveryTokenHash,
                recoveryTokenSalt
            );

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                _userService.CreateLoginPrincipal(user),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    AllowRefresh = true,
                    ExpiresUtc = DateTime.UtcNow.AddDays(1)
                });

            return Ok(
                new
                {
                    Message = "Account Created",
                    For = "Register",
                    Type = "Success",
                    Data = new
                    {
                        UserIdentifier = _userService.GetUserHashId(user.Id),
                        Username = user.Username
                    },
                    JumpTo = Url.Action(
                        action: "Me",
                        controller: "Home",
                        values: new
                        {
                            Area = "Users"
                        }
                    )
                }
            );
        }

        [HttpGet("/users/login")]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost("/users/login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(
            [FromBody] In_Login request
        )
        {
            if (request.UserIdentifier == null || request.Password == null)
            {
                return Ok(
                    new
                    {
                        Message = "Missing Data!",
                        For = "Login",
                        Type = "Error"
                    }
                );
            }

            User? toLogIn = await _userService.GetUserByPartial(request.UserIdentifier);
            if (toLogIn == null || !_userService.VerifyHash(request.Password, toLogIn.PasswordHash, toLogIn.PasswordSalt))
            {
                return Ok(
                    new
                    {
                        Message = "Username or Password invalid!",
                        For = "Login",
                        Type = "Error"
                    }
                );
            }

            if (!await _userService.HasPermission(toLogIn, p => _permissionsService.GetPermissionHashId(p.Id), PagePermissions.Users_Home_Login))
            {
                return Ok(
                    new
                    {
                        Message = "You are not allowed to login!",
                        For = "Login",
                        Type = "Error"
                    }
                );
            }

            //if (await _userService.HasCriticalRole(toLogIn) || await _userService.HasCriticalPermission(toLogIn))
            //{
            //    if (request.mfaCode == null)
            //    {
            //        return Ok(
            //            new
            //            {
            //                Message = "2FA Validation required",
            //                For = "Login",
            //                Type = "Error",
            //                Data = new
            //                {
            //                    UserIdentifier = "@" + toLogIn.Username
            //                },
            //                Tags = new string[]
            //                {
            //                ResponseTags.DO_MFA
            //                }
            //            }
            //        );
            //    }

            //    if (!_mfaService.ValidateMFARequest(_userService.GetUserHashId(toLogIn.Id), request.mfaCode))
            //    {
            //        return Ok(
            //            new
            //            {
            //                Message = "2FA Validation failed",
            //                For = "Login",
            //                Type = "Error",
            //                Data = new
            //                {
            //                    UserIdentifier = "@" + toLogIn.Username
            //                }
            //            }
            //        );
            //    }
            //}

            await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            _userService.CreateLoginPrincipal(toLogIn),
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTime.UtcNow.AddDays(1)
            });


            return Ok(
                new
                {
                    Message = "Logged in successfully",
                    For = "Login",
                    Type = "Success",
                    Data = new
                    {
                        UserIdentifier = _userService.GetUserHashId(toLogIn.Id)
                    },
                    JumpTo = Url.Action(
                        action: "Me",
                        controller: "Home",
                        values: new
                        {
                            Area = "Users"
                        }
                    )
                }
            );
        }



        [HttpGet("/users/logout")]
        [HttpPost("/users/logout")]
        [Authorize]
        public async Task<IActionResult> Logout(
        )
        {
            await HttpContext.SignOutAsync();

            return Ok(
                new
                {
                    Message = "Logged out",
                    For = "Logout",
                    Type = "Success",
                    JumpTo = Url.Action(
                        action: "Index",
                        controller: "Home",
                        values: new
                        {
                            Area = "Home"
                        }
                    )
                }
            );
        }

        [HttpGet("/users/me")]
        [Permission(
            PagePermissions.Users_Home_Me
        )]
        public async Task<IActionResult> Me(
        )
        {
            User? user = await GetIdentity();
            if (user == null)
            {
                return BadRequest();
            }


            //return Ok(string.Join("\n", from role in await _userService.GetUserRoles(user.Id).ToArrayAsync() select $"{_rolesService.GetRoleHashId(role.Id)}: {role.Name}"));
            return View();
        }
    }
}
