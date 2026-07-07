using System.Security.Claims;
using Foundation.Access.Accounts;
using Foundation.Access.Authentication;
using Foundation.Access.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain;
using YourRhythmStudio.Infrastructure.Foundation;
using YourRhythmStudio.ViewModels.Auth;

namespace YourRhythmStudio.Controllers;

public class AuthController : Controller
{
    private const string CookieScheme = "YourRhythmCookie";

    private readonly SaasAccessService _saasAccessService;
    private readonly IUserProfileResolver _userProfileResolver;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        SaasAccessService saasAccessService,
        IUserProfileResolver userProfileResolver,
        IWebHostEnvironment environment)
    {
        _saasAccessService = saasAccessService;
        _userProfileResolver = userProfileResolver;
        _environment = environment;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = new PasswordSignInRequest(
            model.Email,
            model.Password,
            string.IsNullOrWhiteSpace(model.TenantKey) ? null : model.TenantKey);

        var result = await _saasAccessService.SignInWithPasswordAsync(request);
        if (result.IsFailure || result.Value is null)
        {
            ModelState.AddModelError(string.Empty, "E-mail ou senha invalidos.");
            return View(model);
        }

        var session = result.Value;
        var claims = BuildClaims(session);

        // Papel de domínio (escola/professor/aluno) usado para rotear o dashboard.
        // No MVP é resolvido pelas contas demo; admin de plataforma vira platform-admin.
        var profile = await _userProfileResolver.ResolveForSignInAsync(
            session.AccountId,
            session.Email,
            session.DisplayName,
            ResolveFallbackRole(session));
        AddYourRhythmClaims(claims, profile);

        var identity = new ClaimsIdentity(claims, CookieScheme, ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = session.ExpiresAtUtc
            });

        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = CookieScheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    private string? ResolveFallbackRole(IssuedSaasSession session)
    {
        if (_environment.IsDevelopment())
        {
            var demoRole = DemoPersonas.ResolveRole(session.Email);
            if (demoRole is not null)
            {
                return demoRole;
            }
        }

        return session.PlatformRole == PlatformAccessRole.PlatformAdmin
            ? YourRhythmRoles.PlatformAdmin
            : null;
    }

    private static void AddYourRhythmClaims(List<Claim> claims, AuthenticatedUserProfile? profile)
    {
        if (profile is null)
        {
            return;
        }

        claims.Add(new Claim(UserProfileResolver.RoleClaim, profile.Role));
        claims.Add(new Claim(ClaimTypes.Role, profile.Role));

        AddGuidClaim(claims, UserProfileResolver.SchoolIdClaim, profile.SchoolId);
        AddGuidClaim(claims, UserProfileResolver.SchoolUserIdClaim, profile.SchoolUserId);
        AddGuidClaim(claims, UserProfileResolver.TeacherProfileIdClaim, profile.TeacherProfileId);
        AddGuidClaim(claims, UserProfileResolver.StudentProfileIdClaim, profile.StudentProfileId);
    }

    private static void AddGuidClaim(List<Claim> claims, string type, Guid? value)
    {
        if (value is not null)
        {
            claims.Add(new Claim(type, value.Value.ToString()));
        }
    }

    private static List<Claim> BuildClaims(IssuedSaasSession session)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.AccountId.ToString()),
            new(ClaimTypes.Name, session.DisplayName),
            new(ClaimTypes.Email, session.Email),
            new("SessionId", session.SessionId.ToString()),
            new("PlatformRole", session.PlatformRole.ToString())
        };

        if (session.PlatformRole != PlatformAccessRole.None)
        {
            claims.Add(new Claim(ClaimTypes.Role, session.PlatformRole.ToString()));
        }

        if (session.TenantId is not null)
        {
            claims.Add(new Claim("TenantId", session.TenantId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(session.TenantDisplayName))
        {
            claims.Add(new Claim("TenantDisplayName", session.TenantDisplayName));
        }

        if (session.TenantRole is not null)
        {
            claims.Add(new Claim("TenantRole", session.TenantRole.Value.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, session.TenantRole.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(session.PlanCode))
        {
            claims.Add(new Claim("PlanCode", session.PlanCode));
        }

        foreach (var feature in session.EnabledFeatures)
        {
            claims.Add(new Claim("EnabledFeature", feature));
        }

        return claims;
    }
}
