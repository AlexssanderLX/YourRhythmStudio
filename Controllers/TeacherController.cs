using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourRhythmStudio.Application.Learning;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain;
using YourRhythmStudio.ViewModels.Learning;

namespace YourRhythmStudio.Controllers;

[Authorize(AuthenticationSchemes = "YourRhythmCookie", Roles = YourRhythmRoles.Teacher)]
[Route("Teacher")]
public sealed class TeacherController : Controller
{
    private readonly IUserProfileResolver _profileResolver;
    private readonly TeacherStudentService _teacherStudentService;
    private readonly LessonService _lessonService;
    private readonly RepertoireService _repertoireService;
    private readonly AssignmentService _assignmentService;
    private readonly FeedbackService _feedbackService;

    public TeacherController(
        IUserProfileResolver profileResolver,
        TeacherStudentService teacherStudentService,
        LessonService lessonService,
        RepertoireService repertoireService,
        AssignmentService assignmentService,
        FeedbackService feedbackService)
    {
        _profileResolver = profileResolver;
        _teacherStudentService = teacherStudentService;
        _lessonService = lessonService;
        _repertoireService = repertoireService;
        _assignmentService = assignmentService;
        _feedbackService = feedbackService;
    }

    [HttpGet("")]
    [HttpGet("Dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var summary = await _teacherStudentService.GetTeacherDashboardAsync(profile, cancellationToken);
        return View(new TeacherDashboardViewModel { Summary = summary });
    }

    [HttpGet("Students")]
    public async Task<IActionResult> Students(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var students = await _teacherStudentService.ListStudentsAsync(profile, cancellationToken);
        return View(new TeacherStudentsViewModel { Students = students });
    }

    [HttpGet("Students/Create")]
    public IActionResult CreateStudent()
    {
        return View(new CreateStudentViewModel());
    }

    [HttpPost("Students/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStudent(CreateStudentViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var profile = await CurrentProfile(cancellationToken);
        var student = await _teacherStudentService.CreateStudentAsync(
            profile,
            new CreateTeacherStudentRequest(
                model.DisplayName,
                model.Email,
                model.Instrument,
                model.Level,
                model.Notes),
            cancellationToken);

        return RedirectToAction(nameof(StudentDetail), new { studentId = student.StudentProfileId });
    }

    [HttpGet("Students/{studentId:guid}")]
    public async Task<IActionResult> StudentDetail(Guid studentId, CancellationToken cancellationToken)
    {
        var detail = await LoadStudentDetail(studentId, cancellationToken);
        return detail is null ? Forbid() : View(detail);
    }

    [HttpPost("Students/{studentId:guid}/Lessons")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLesson(
        Guid studentId,
        [Bind(Prefix = "Lesson")] CreateLessonViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.StudentProfileId != studentId)
        {
            TempData["Error"] = "Dados da aula invalidos.";
            return RedirectToAction(nameof(StudentDetail), new { studentId });
        }

        var profile = await CurrentProfile(cancellationToken);
        await _lessonService.CreateLessonAsync(
            profile,
            new CreateLessonRequest(studentId, model.Title, model.LessonDateUtc, model.Notes),
            cancellationToken);

        return RedirectToAction(nameof(StudentDetail), new { studentId });
    }

    [HttpPost("Students/{studentId:guid}/Repertoire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRepertoire(
        Guid studentId,
        [Bind(Prefix = "Repertoire")] AddRepertoireViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.StudentProfileId != studentId)
        {
            TempData["Error"] = "Dados do repertorio invalidos.";
            return RedirectToAction(nameof(StudentDetail), new { studentId });
        }

        var profile = await CurrentProfile(cancellationToken);
        await _repertoireService.AddRepertoireAsync(
            profile,
            new AddRepertoireRequest(
                studentId,
                model.Title,
                model.ComposerOrArtist,
                model.Instrument,
                model.Level,
                model.Notes,
                model.ReferenceUrl),
            cancellationToken);

        return RedirectToAction(nameof(StudentDetail), new { studentId });
    }

    [HttpPost("Students/{studentId:guid}/Assignments")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAssignment(
        Guid studentId,
        [Bind(Prefix = "Assignment")] CreateAssignmentViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.StudentProfileId != studentId)
        {
            TempData["Error"] = "Dados da missao invalidos.";
            return RedirectToAction(nameof(StudentDetail), new { studentId });
        }

        var profile = await CurrentProfile(cancellationToken);
        await _assignmentService.CreateAssignmentAsync(
            profile,
            new CreateAssignmentRequest(
                studentId,
                model.Title,
                model.Description,
                model.DueAtUtc,
                model.TargetMinutes,
                model.XpReward),
            cancellationToken);

        return RedirectToAction(nameof(StudentDetail), new { studentId });
    }

    [HttpPost("Students/{studentId:guid}/Feedback")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFeedback(
        Guid studentId,
        [Bind(Prefix = "Feedback")] CreateFeedbackViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.StudentProfileId != studentId)
        {
            TempData["Error"] = "Dados do feedback invalidos.";
            return RedirectToAction(nameof(StudentDetail), new { studentId });
        }

        var profile = await CurrentProfile(cancellationToken);
        await _feedbackService.CreateFeedbackAsync(
            profile,
            new CreateFeedbackRequest(studentId, model.Message, model.VisibleToStudent),
            cancellationToken);

        return RedirectToAction(nameof(StudentDetail), new { studentId });
    }

    private async Task<TeacherStudentDetailViewModel?> LoadStudentDetail(Guid studentId, CancellationToken cancellationToken)
    {
        try
        {
            var profile = await CurrentProfile(cancellationToken);
            var detail = await _teacherStudentService.GetStudentDetailAsync(profile, studentId, cancellationToken);
            return new TeacherStudentDetailViewModel
            {
                Detail = detail,
                Lesson = new CreateLessonViewModel { StudentProfileId = studentId, LessonDateUtc = DateTime.UtcNow },
                Repertoire = new AddRepertoireViewModel { StudentProfileId = studentId, Instrument = detail.Student.Instrument, Level = detail.Student.Level },
                Assignment = new CreateAssignmentViewModel { StudentProfileId = studentId, XpReward = 50 },
                Feedback = new CreateFeedbackViewModel { StudentProfileId = studentId, VisibleToStudent = true }
            };
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    private Task<AuthenticatedUserProfile> CurrentProfile(CancellationToken cancellationToken)
    {
        return _profileResolver.ResolveCurrentAsync(User, cancellationToken);
    }
}
