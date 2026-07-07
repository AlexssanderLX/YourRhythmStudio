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
            YourRhythmRoles.Student => View("Student"),
            YourRhythmRoles.Teacher => View("Teacher"),
            YourRhythmRoles.SchoolOwner or YourRhythmRoles.SchoolAdmin => View("School"),
            _ => View("School"),
        };
    }

    // ---------- Teacher pages ----------

    [HttpGet]
    public IActionResult Students()
    {
        RequireRole(YourRhythmRoles.Teacher);
        return View("TeacherStudents");
    }

    [HttpGet]
    public IActionResult StudentDetail(int id)
    {
        RequireRole(YourRhythmRoles.Teacher);
        ViewData["StudentId"] = id;
        return View("TeacherStudentDetail");
    }

    [HttpGet]
    public IActionResult Lessons()
    {
        RequireRole(YourRhythmRoles.Teacher);
        return View("TeacherLessons");
    }

    [HttpGet]
    public IActionResult Missions()
    {
        RequireRole(YourRhythmRoles.Teacher);
        return View("TeacherMissions");
    }

    // ---------- Student pages ----------

    [HttpGet]
    public IActionResult Repertoire()
    {
        RequireRole(YourRhythmRoles.Student);
        return View("StudentRepertoire");
    }

    [HttpGet]
    public IActionResult StudentMissions()
    {
        RequireRole(YourRhythmRoles.Student);
        return View("StudentMissions");
    }

    [HttpGet]
    public IActionResult Achievements()
    {
        RequireRole(YourRhythmRoles.Student);
        return View("StudentAchievements");
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

    private void RequireRole(string role)
    {
        var actual = User.FindFirst("YourRhythmRole")?.Value;
        if (actual != role)
            Response.Redirect("/dashboard");
    }
}
