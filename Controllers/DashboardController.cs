using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain;
using YourRhythmStudio.ViewModels.Settings;

namespace YourRhythmStudio.Controllers;

[Authorize(AuthenticationSchemes = "YourRhythmCookie")]
public class DashboardController : Controller
{
    private const string CookieScheme = "YourRhythmCookie";

    private readonly IUserProfileResolver _profileResolver;
    private readonly SettingsService _settingsService;

    public DashboardController(IUserProfileResolver profileResolver, SettingsService settingsService)
    {
        _profileResolver = profileResolver;
        _settingsService = settingsService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var role = CurrentRole();
        return role switch
        {
            YourRhythmRoles.Student => RedirectToAction("Dashboard", "Student"),
            YourRhythmRoles.Teacher => RedirectToAction("Dashboard", "Teacher"),
            _ => RedirectToAction("Index", "Home"),
        };
    }

    [HttpGet]
    public IActionResult Teacher()
    {
        return RequireRole(YourRhythmRoles.Teacher)
            ? RedirectToAction("Dashboard", "Teacher")
            : RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Student()
    {
        return RequireRole(YourRhythmRoles.Student)
            ? RedirectToAction("Dashboard", "Student")
            : RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Students()
    {
        return RequireRole(YourRhythmRoles.Teacher)
            ? RedirectToAction("Students", "Teacher")
            : Forbid();
    }

    [HttpGet]
    public IActionResult StudentDetail(Guid id)
    {
        return RequireRole(YourRhythmRoles.Teacher)
            ? RedirectToAction("StudentDetail", "Teacher", new { studentId = id })
            : Forbid();
    }

    [HttpGet]
    public IActionResult Lessons()
    {
        return RequireRole(YourRhythmRoles.Teacher)
            ? RedirectToAction("Students", "Teacher")
            : Forbid();
    }

    [HttpGet]
    public IActionResult Missions()
    {
        return RequireRole(YourRhythmRoles.Teacher)
            ? RedirectToAction("Students", "Teacher")
            : Forbid();
    }

    [HttpGet]
    public IActionResult Repertoire()
    {
        return RequireRole(YourRhythmRoles.Student)
            ? RedirectToAction("Repertoire", "Student")
            : Forbid();
    }

    [HttpGet]
    public IActionResult StudentMissions()
    {
        return RequireRole(YourRhythmRoles.Student)
            ? RedirectToAction("Assignments", "Student")
            : Forbid();
    }

    [HttpGet]
    public IActionResult Achievements()
    {
        return RequireRole(YourRhythmRoles.Student)
            ? RedirectToAction("Progress", "Student")
            : Forbid();
    }

    [HttpGet]
    public IActionResult Teachers()
    {
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult SchoolStudents()
    {
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Settings(CancellationToken ct)
    {
        var profile = await _profileResolver.ResolveCurrentAsync(User, ct);
        var role = profile.Role switch
        {
            YourRhythmRoles.Student => "student",
            YourRhythmRoles.Teacher => "teacher",
            _ => "school"
        };

        ViewData["Role"] = role;
        ViewData["Title"] = "Conta";
        ViewData["DashTitle"] = "Conta";

        if (profile.Role == YourRhythmRoles.Student)
        {
            var account = await _settingsService.GetStudentAccountAsync(profile, ct);
            return View("Settings", new AccountSettingsPageViewModel
            {
                Role = role,
                Student = new StudentAccountSettingsViewModel
                {
                    Profile = new StudentProfileFormViewModel
                    {
                        DisplayName = account.DisplayName,
                        ExternalContact = account.ExternalContact,
                        Instrument = account.Instrument,
                        CurrentLevel = account.CurrentLevel,
                        CurrentLevelBadge = account.CurrentLevelBadge,
                        ProfilePhotoUrl = account.ProfilePhotoUrl
                    }
                }
            });
        }

        if (profile.Role == YourRhythmRoles.Teacher)
        {
            var account = await _settingsService.GetTeacherAccountAsync(profile, ct);
            return View("Settings", new AccountSettingsPageViewModel
            {
                Role = role,
                Teacher = new TeacherAccountSettingsViewModel
                {
                    Profile = new TeacherProfilePhotoFormViewModel
                    {
                        DisplayName = account.DisplayName,
                        Email = account.Email,
                        ProfilePhotoUrl = account.ProfilePhotoUrl
                    },
                    Email = new TeacherEmailFormViewModel
                    {
                        CurrentEmail = account.Email,
                        NewEmail = account.Email
                    },
                    Password = new TeacherPasswordFormViewModel()
                }
            });
        }

        return View("Settings", new AccountSettingsPageViewModel { Role = role });
    }

    [HttpPost("Dashboard/Settings/StudentProfile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveStudentProfile(StudentProfileFormViewModel model, CancellationToken ct)
    {
        var profile = await _profileResolver.ResolveCurrentAsync(User, ct);
        if (profile.Role != YourRhythmRoles.Student)
            return Forbid();

        if (!ModelState.IsValid)
        {
            TempData["SettingsError"] = FirstModelError();
            return RedirectToAction(nameof(Settings));
        }

        try
        {
            var result = await _settingsService.UpdateStudentAccountAsync(
                profile,
                new UpdateStudentAccountRequest(model.DisplayName, model.ExternalContact, model.Photo, model.RemovePhoto),
                ct);
            await RefreshIdentityAsync(result);
            TempData["SettingsSuccess"] = "Perfil atualizado.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["SettingsError"] = ex.Message;
        }

        return RedirectToAction(nameof(Settings));
    }

    [HttpPost("Dashboard/Settings/TeacherPhoto")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTeacherPhoto(TeacherProfilePhotoFormViewModel model, CancellationToken ct)
    {
        var profile = await _profileResolver.ResolveCurrentAsync(User, ct);
        if (profile.Role != YourRhythmRoles.Teacher)
            return Forbid();

        try
        {
            var result = await _settingsService.UpdateTeacherPhotoAsync(
                profile,
                new UpdateTeacherPhotoRequest(model.Photo, model.RemovePhoto),
                ct);
            await RefreshIdentityAsync(result);
            TempData["SettingsSuccess"] = "Foto atualizada.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["SettingsError"] = ex.Message;
        }

        return RedirectToAction(nameof(Settings), null, "profile");
    }

    [HttpPost("Dashboard/Settings/TeacherEmail")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeTeacherEmail(TeacherEmailFormViewModel model, CancellationToken ct)
    {
        var profile = await _profileResolver.ResolveCurrentAsync(User, ct);
        if (profile.Role != YourRhythmRoles.Teacher)
            return Forbid();

        if (!ModelState.IsValid)
        {
            TempData["SettingsError"] = FirstModelError();
            return RedirectToAction(nameof(Settings), null, "email");
        }

        try
        {
            var result = await _settingsService.ChangeTeacherEmailAsync(
                profile,
                new ChangeTeacherEmailRequest(model.NewEmail, model.CurrentPassword),
                ct);
            await RefreshIdentityAsync(result);
            TempData["SettingsSuccess"] = "E-mail de acesso atualizado.";
        }
        catch (UnauthorizedAccessException)
        {
            TempData["SettingsError"] = "Senha atual incorreta.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["SettingsError"] = ex.Message;
        }

        return RedirectToAction(nameof(Settings), null, "email");
    }

    [HttpPost("Dashboard/Settings/TeacherPassword")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeTeacherPassword(TeacherPasswordFormViewModel model, CancellationToken ct)
    {
        var profile = await _profileResolver.ResolveCurrentAsync(User, ct);
        if (profile.Role != YourRhythmRoles.Teacher)
            return Forbid();

        if (!ModelState.IsValid)
        {
            TempData["SettingsError"] = FirstModelError();
            return RedirectToAction(nameof(Settings), null, "security");
        }

        try
        {
            var result = await _settingsService.ChangeTeacherPasswordAsync(
                profile,
                new ChangeTeacherPasswordRequest(model.CurrentPassword, model.NewPassword),
                ct);
            await RefreshIdentityAsync(result);
            TempData["SettingsSuccess"] = "Senha atualizada com seguranca.";
        }
        catch (UnauthorizedAccessException)
        {
            TempData["SettingsError"] = "Senha atual incorreta.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["SettingsError"] = ex.Message;
        }

        return RedirectToAction(nameof(Settings), null, "security");
    }

    private bool RequireRole(string role) => CurrentRole() == role;

    private bool RequireSchoolRole() =>
        CurrentRole() is YourRhythmRoles.SchoolOwner or YourRhythmRoles.SchoolAdmin;

    private string? CurrentRole() => User.FindFirst("YourRhythmRole")?.Value;

    private string FirstModelError()
        => ModelState.Values.SelectMany(value => value.Errors).FirstOrDefault()?.ErrorMessage
            ?? "Verifique os dados informados.";

    private async Task RefreshIdentityAsync(CredentialUpdateResult result)
    {
        var claims = User.Claims
            .Where(claim => claim.Type != ClaimTypes.Name && claim.Type != ClaimTypes.Email)
            .ToList();
        claims.Add(new Claim(ClaimTypes.Name, result.DisplayName));
        claims.Add(new Claim(ClaimTypes.Email, result.Email));

        var identity = new ClaimsIdentity(claims, CookieScheme, ClaimTypes.Name, ClaimTypes.Role);
        await HttpContext.SignInAsync(
            CookieScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });
    }
}
