using Market.Client.API.Interface.Auth;
using Market.Client.Models.Auth;
using Market.Shared.Contracts.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Market.Client.Controllers
{
    public class AccountController : BaseController
    {
        private readonly IAuthApiClient _authApiClient;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAuthApiClient authApiClient, ILogger<AccountController> logger)
        {
            _authApiClient = authApiClient;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var (succeeded, result, error) = await _authApiClient.LoginAsync(new LoginRequest(model.EmailOrUsername, model.Password));

            if (!succeeded || result is null || string.IsNullOrEmpty(result.Token))
            {
                ModelState.AddModelError(string.Empty, error ?? "Login failed.");
                return View(model);
            }

            HttpContext.Session.SetString("JwtToken", result.Token);
            HttpContext.Session.SetString("AccessToken", result.Token);
            HttpContext.Session.SetString("RefreshToken", result.RefreshToken);
            HttpContext.Session.SetString("UserFullName", result.FullName);

            // Store user profile image URL if available
            if (!string.IsNullOrEmpty(result.ImageUrl))
            {
                HttpContext.Session.SetString("UserImageUrl", result.ImageUrl);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, result.UserId.ToString()),
                new(ClaimTypes.Name, result.FullName),
                new(ClaimTypes.Email, result.Email),
                new("access_token", result.Token)
            };
            claims.AddRange(result.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var authProperties = new AuthenticationProperties();
            authProperties.StoreTokens(new[]
            {
                new AuthenticationToken { Name = "access_token", Value = result.Token }
            });

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            TempData["SuccessMessage"] = $"Welcome back, {result.FullName}!";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var (succeeded, userId, error) = await _authApiClient.RegisterAsync(
                new RegisterRequest(model.FullName, model.Username, model.Email, model.Password, null));

            if (!succeeded)
            {
                ModelState.AddModelError(string.Empty, error ?? "Registration failed.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Account created successfully. Please log in.";
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
