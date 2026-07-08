using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain;

namespace YourRhythmStudio.Controllers;

[Authorize(AuthenticationSchemes = "YourRhythmCookie")]
public class DashboardController : Controller
{
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
            _ => View("School"),
        };
    }

    // ---------- Teacher pages ----------

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

    // ---------- Student pages ----------

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

    // ---------- School pages ----------

    [HttpGet]
    public IActionResult Teachers()
    {
        return RequireSchoolRole()
            ? View("SchoolTeachers")
            : Forbid();
    }

    [HttpGet]
    public IActionResult SchoolStudents()
    {
        return RequireSchoolRole()
            ? View("SchoolStudents")
            : Forbid();
    }

    // ---------- Settings ----------

    [HttpGet]
    public async Task<IActionResult> Settings(CancellationToken ct)
    {
        var profile = await _profileResolver.ResolveCurrentAsync(User, ct);

        ViewData["Role"] = profile.Role switch
        {
            YourRhythmRoles.Student => "student",
            YourRhythmRoles.Teacher => "teacher",
            _ => "school"
        };
        ViewData["Title"] = "Configurações";
        ViewData["DashTitle"] = "Configurações";

        if (profile.SchoolUserId.HasValue)
            ViewData["Account"] = await _settingsService.GetAccountAsync(profile.SchoolUserId.Value, ct);

        if (profile.TeacherProfileId.HasValue)
            ViewData["TeacherProfile"] = await _settingsService.GetTeacherProfileAsync(profile.TeacherProfileId.Value, ct);

        if ((profile.Role == YourRhythmRoles.SchoolOwner || profile.Role == YourRhythmRoles.SchoolAdmin)
            && profile.SchoolId.HasValue)
            ViewData["SchoolInfo"] = await _settingsService.GetSchoolAsync(profile.SchoolId.Value, ct);

        return View("Settings");
    }

    [HttpPost("Dashboard/Settings/Account")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAccount(
        string displayName, string? phone, string? city, CancellationToken ct)
    {
        var profile = await _profileResolver.ResolveCurrentAsync(User, ct);
        if (profile.SchoolUserId.HasValue && !string.IsNullOrWhiteSpace(displayName))
            await _settingsService.SaveAccountAsync(profile.SchoolUserId.Value, displayName, phone, city, ct);

        TempData["SettingsSuccess"] = "Conta atualizada.";
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost("Dashboard/Settings/TeacherProfile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTeacherProfile(
        string? instrumentFocus, string? bio, CancellationToken ct)
    {
        var profile = await _profileResolver.ResolveCurrentAsync(User, ct);
        if (profile.TeacherProfileId.HasValue)
            await _settingsService.SaveTeacherProfileAsync(
                profile.TeacherProfileId.Value,
                instrumentFocus ?? string.Empty,
                bio ?? string.Empty,
                ct);

        TempData["SettingsSuccess"] = "Perfil de professor atualizado.";
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost("Dashboard/Settings/Studio")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveStudio(string? schoolName, CancellationToken ct)
    {
        var profile = await _profileResolver.ResolveCurrentAsync(User, ct);
        if (profile.SchoolId.HasValue && !string.IsNullOrWhiteSpace(schoolName))
            await _settingsService.SaveSchoolNameAsync(profile.SchoolId.Value, schoolName, ct);

        TempData["SettingsSuccess"] = "Informações do studio atualizadas.";
        return RedirectToAction(nameof(Settings));
    }

    private bool RequireRole(string role) => CurrentRole() == role;

    private bool RequireSchoolRole() =>
        CurrentRole() is YourRhythmRoles.SchoolOwner or YourRhythmRoles.SchoolAdmin;

    private string? CurrentRole() => User.FindFirst("YourRhythmRole")?.Value;
}
