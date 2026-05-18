using System.Security.Claims;
using Fiap.SecureSystem.WebApp.Authentication;
using Fiap.SecureSystem.WebApp.Clients.Identity;
using Fiap.SecureSystem.WebApp.Clients.Identity.Contracts;
using Fiap.SecureSystem.WebApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fiap.SecureSystem.WebApp.Controllers;

[AllowAnonymous]
public sealed class AuthController(IIdentityServiceClient identityServiceClient) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        return View(new RegisterViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var response = await identityServiceClient.LoginAsync(model.Email, model.Password, cancellationToken);

            await SignInAsync(response);

            return RedirectToLocal(model.ReturnUrl);
        }
        catch (IdentityServiceClientException exception)
        {
            model.ErrorMessage = exception.Message;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var response = await identityServiceClient.RegisterAsync(
                model.Email,
                model.DisplayName,
                model.Password,
                cancellationToken);

            await SignInAsync(response);

            return RedirectToLocal(model.ReturnUrl);
        }
        catch (IdentityServiceClientException exception)
        {
            model.ErrorMessage = exception.Message;
            return View(model);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Dashboard", "Home");
    }

    private async Task SignInAsync(LoginResponse response)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, response.User.Id.ToString()),
            new(ClaimTypes.Name, response.User.Name),
            new(ClaimTypes.Email, response.User.Email),
            new(WebAppClaimTypes.AccessToken, response.AccessToken)
        };

        claims.AddRange(response.User.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(response.User.Scopes.Select(scope => new Claim(WebAppClaimTypes.Scope, scope)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn)
            });
    }
}
