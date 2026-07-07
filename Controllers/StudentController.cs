using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourRhythmStudio.Application.Learning;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain;
using YourRhythmStudio.ViewModels.Learning;

namespace YourRhythmStudio.Controllers;

[Authorize(AuthenticationSchemes = "YourRhythmCookie", Roles = YourRhythmRoles.Student)]
[Route("Student")]
public sealed class StudentController : Controller
{
    private readonly IUserProfileResolver _profileResolver;
    private readonly AssignmentService _assignmentService;
    private readonly RepertoireService _repertoireService;
    private readonly FeedbackService _feedbackService;
    private readonly ProgressService _progressService;

    public StudentController(
        IUserProfileResolver profileResolver,
        AssignmentService assignmentService,
        RepertoireService repertoireService,
        FeedbackService feedbackService,
        ProgressService progressService)
    {
        _profileResolver = profileResolver;
        _assignmentService = assignmentService;
        _repertoireService = repertoireService;
        _feedbackService = feedbackService;
        _progressService = progressService;
    }

    [HttpGet("")]
    [HttpGet("Dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var summary = await _progressService.GetStudentDashboardAsync(profile, cancellationToken);
        return View(new StudentDashboardViewModel { Summary = summary });
    }

    [HttpGet("Assignments")]
    public async Task<IActionResult> Assignments(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var assignments = await _assignmentService.ListForCurrentStudentAsync(profile, cancellationToken);
        return View(new StudentAssignmentsViewModel { Assignments = assignments });
    }

    [HttpPost("Assignments/{assignmentId:guid}/Start")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartAssignment(Guid assignmentId, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        await _assignmentService.StartAssignmentAsync(profile, assignmentId, cancellationToken);
        return RedirectToAction(nameof(Assignments));
    }

    [HttpPost("Assignments/{assignmentId:guid}/Complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteAssignment(Guid assignmentId, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        await _assignmentService.CompleteAssignmentAsync(profile, assignmentId, cancellationToken);
        return RedirectToAction(nameof(Assignments));
    }

    [HttpGet("Repertoire")]
    public async Task<IActionResult> Repertoire(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var repertoire = await _repertoireService.ListForCurrentStudentAsync(profile, cancellationToken);
        return View(new StudentRepertoireViewModel { Repertoire = repertoire });
    }

    [HttpGet("Feedback")]
    public async Task<IActionResult> Feedback(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var feedback = await _feedbackService.ListVisibleForCurrentStudentAsync(profile, cancellationToken);
        return View(new StudentFeedbackViewModel { Feedback = feedback });
    }

    [HttpGet("Progress")]
    public async Task<IActionResult> Progress(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var progress = await _progressService.GetCurrentStudentProgressAsync(profile, cancellationToken);
        return View(new StudentProgressViewModel { Progress = progress });
    }

    private Task<AuthenticatedUserProfile> CurrentProfile(CancellationToken cancellationToken)
    {
        return _profileResolver.ResolveCurrentAsync(User, cancellationToken);
    }
}

