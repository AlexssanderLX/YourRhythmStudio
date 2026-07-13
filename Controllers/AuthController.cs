using System.Security.Claims;
using System.Text.RegularExpressions;
using Foundation.Access.Abstractions;
using Foundation.Access.Accounts;
using Foundation.Access.Authentication;
using Foundation.Access.Security;
using Foundation.Access.Services;
using Foundation.Core.Abstractions;
using Foundation.SecureLinks.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Application.Root;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain;
using YourRhythmStudio.Domain.Users;
using YourRhythmStudio.Infrastructure.Data;
using YourRhythmStudio.Infrastructure.Foundation;
using YourRhythmStudio.ViewModels.Auth;

namespace YourRhythmStudio.Controllers;

public class AuthController : Controller
{
    private const string CookieScheme = "YourRhythmCookie";

    private readonly SaasAccessService _saasAccessService;
    private readonly IUserProfileResolver _userProfileResolver;
    private readonly IAccountStore _accountStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClock _clock;
    private readonly YourRhythmDbContext _db;
    private readonly SecureLinkService _secureLinkService;
    private readonly AccessRequestService _accessRequestService;
    private readonly RootAdminService _rootAdminService;

    public AuthController(
        SaasAccessService saasAccessService,
        IUserProfileResolver userProfileResolver,
        IAccountStore accountStore,
        IPasswordHasher passwordHasher,
        IClock clock,
        YourRhythmDbContext db,
        SecureLinkService secureLinkService,
        AccessRequestService accessRequestService,
        RootAdminService rootAdminService)
    {
        _saasAccessService = saasAccessService;
        _userProfileResolver = userProfileResolver;
        _accountStore = accountStore;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _db = db;
        _secureLinkService = secureLinkService;
        _accessRequestService = accessRequestService;
        _rootAdminService = rootAdminService;
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

        if (session.PlatformRole == PlatformAccessRole.PlatformAdmin && string.IsNullOrWhiteSpace(model.ReturnUrl))
            return RedirectToAction("Index", "Root");

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
    public async Task<IActionResult> Register(string? plan = null, CancellationToken cancellationToken = default)
        => View(await _accessRequestService.BuildRegisterViewModelAsync(plan, cancellationToken));

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.PlanOptions = await _accessRequestService.GetRequestablePlanOptionsAsync(cancellationToken);
            return View(model);
        }

        var result = await _accessRequestService.SubmitAsync(model, cancellationToken);
        if (!result.Success)
        {
            if (!string.IsNullOrWhiteSpace(result.MemberName))
                ModelState.AddModelError(result.MemberName, result.Error ?? "Verifique os dados informados.");
            else
                ModelState.AddModelError(string.Empty, result.Error ?? "Verifique os dados informados.");

            model.PlanOptions = await _accessRequestService.GetRequestablePlanOptionsAsync(cancellationToken);
            return View(model);
        }

        if (!result.NotificationSent && !result.IsDuplicate)
            TempData["RequestWarning"] = "Solicitacao recebida, mas a notificacao ao administrador precisa ser verificada.";

        return RedirectToAction(nameof(RequestSent));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult RequestSent() => View();

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> SetPassword(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction(nameof(Login));

        var request = await _rootAdminService.FindBySetPasswordTokenAsync(token);
        if (request is null)
            return View("SetPasswordInvalid");

        return View(new SetPasswordViewModel { Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, error) = await _rootAdminService.SetPasswordAsync(model.Token, model.Password);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Operacao invalida.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Senha definida com sucesso! Faca login para acessar o sistema.";
        return RedirectToAction(nameof(Login));
    }

    private static string GenerateSlug(string name)
    {
        var s = name.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"[^a-z0-9\s-]", "");
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, @"-+", "-").Trim('-');
        return s.Length > 80 ? s[..80] : s;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> StudentAccess(string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return RedirectToAction(nameof(Login));

        var baseUri = new Uri($"{Request.Scheme}://{Request.Host}");
        var resolution = await _secureLinkService.ResolveAsync(baseUri, code, cancellationToken);

        if (resolution.IsFailure || !Guid.TryParse(resolution.Value?.ResourceKey, out var studentProfileId))
            return RedirectToAction(nameof(Login));

        var student = await _db.StudentProfiles
            .AsNoTracking()
            .Include(s => s.SchoolUser)
            .FirstOrDefaultAsync(s => s.Id == studentProfileId, cancellationToken);

        if (student?.SchoolUser is null || !student.SchoolUser.IsActive)
            return RedirectToAction(nameof(Login));

        var schoolUser = student.SchoolUser;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, schoolUser.Id.ToString()),
            new(ClaimTypes.Name, schoolUser.DisplayName),
            new(ClaimTypes.Email, schoolUser.Email),
            new("SessionId", Guid.NewGuid().ToString()),
            new("PlatformRole", PlatformAccessRole.None.ToString()),
            new(UserProfileResolver.RoleClaim, YourRhythmRoles.Student),
            new(ClaimTypes.Role, YourRhythmRoles.Student),
            new(UserProfileResolver.SchoolIdClaim, student.SchoolId.ToString()),
            new(UserProfileResolver.SchoolUserIdClaim, schoolUser.Id.ToString()),
            new(UserProfileResolver.StudentProfileIdClaim, student.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieScheme, ClaimTypes.Name, ClaimTypes.Role);
        await HttpContext.SignInAsync(
            CookieScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) });

        return RedirectToAction("Dashboard", "Student");
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
        return session.PlatformRole == PlatformAccessRole.PlatformAdmin
            ? YourRhythmRoles.RootAdmin
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
