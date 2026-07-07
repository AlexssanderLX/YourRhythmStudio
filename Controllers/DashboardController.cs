using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourRhythmStudio.Domain;

namespace YourRhythmStudio.Controllers;

[Authorize(AuthenticationSchemes = "YourRhythmCookie")]
public class DashboardController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var role = User.FindFirst("YourRhythmRole")?.Value;

        return role switch
        {
            YourRhythmRoles.Student => RedirectToAction("Dashboard", "Student"),
            YourRhythmRoles.Teacher => RedirectToAction("Dashboard", "Teacher"),
            YourRhythmRoles.SchoolOwner or YourRhythmRoles.SchoolAdmin => View("School"),
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
        return View("SchoolTeachers");
    }

    [HttpGet]
    public IActionResult SchoolStudents()
    {
        return View("SchoolStudents");
    }

    [HttpGet]
    public IActionResult Plan()
    {
        return View("SchoolPlan");
    }

    [HttpGet]
    public IActionResult Settings() => View("Settings");

    private bool RequireRole(string role)
    {
        var actual = User.FindFirst("YourRhythmRole")?.Value;
        return actual == role;
    }
}
