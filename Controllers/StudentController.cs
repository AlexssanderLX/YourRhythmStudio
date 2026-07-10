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
    private readonly SkillService _skillService;
    private readonly LevelConfigService _levelConfigService;

    public StudentController(
        IUserProfileResolver profileResolver,
        AssignmentService assignmentService,
        RepertoireService repertoireService,
        FeedbackService feedbackService,
        ProgressService progressService,
        SkillService skillService,
        LevelConfigService levelConfigService)
    {
        _profileResolver = profileResolver;
        _assignmentService = assignmentService;
        _repertoireService = repertoireService;
        _feedbackService = feedbackService;
        _progressService = progressService;
        _skillService = skillService;
        _levelConfigService = levelConfigService;
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

    [HttpGet("Repertoire/{id:guid}")]
    public async Task<IActionResult> RepertoireDetail(Guid id, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var item = await _repertoireService.GetByIdForCurrentStudentAsync(profile, id, cancellationToken);
        if (item is null) return NotFound();
        return View(new StudentRepertoireDetailViewModel { Item = item });
    }

    [HttpGet("Repertoire/{id:guid}/OpenReference")]
    public async Task<IActionResult> OpenRepertoireReference(Guid id, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var referenceUrl = await _repertoireService.OpenReferenceForCurrentStudentAsync(profile, id, cancellationToken);
        if (referenceUrl is null) return NotFound();
        return Redirect(referenceUrl);
    }

    [HttpGet("Repertoire/{id:guid}/Audio")]
    public async Task<IActionResult> RepertoireAudio(Guid id, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var audio = await _repertoireService.GetAudioForCurrentStudentAsync(profile, id, cancellationToken);
        if (audio is null) return NotFound();
        return PhysicalFile(audio.PhysicalPath, audio.ContentType, audio.DownloadFileName, enableRangeProcessing: true);
    }

    [HttpPost("Repertoire/{id:guid}/Complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteRepertoire(Guid id, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var completed = await _repertoireService.CompleteForCurrentStudentAsync(profile, id, cancellationToken);
        if (!completed) return NotFound();
        return RedirectToAction(nameof(RepertoireDetail), new { id });
    }

    [HttpGet("Levels")]
    public async Task<IActionResult> Levels(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var progress = await _progressService.GetCurrentStudentProgressAsync(profile, cancellationToken);
        var skills = await _skillService.GetStudentSkillsForStudentAsync(profile, cancellationToken);
        return View(new StudentLevelsViewModel
        {
            Progress = progress,
            Skills = skills
        });
    }

    [HttpGet("PendingLevelUp")]
    public async Task<IActionResult> PendingLevelUp(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var ev = await _levelConfigService.GetPendingLevelUpAsync(profile, cancellationToken);
        return Json(ev);
    }

    [HttpPost("LevelUp/{eventId:guid}/Dismiss")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DismissLevelUp(Guid eventId, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        await _levelConfigService.DismissLevelUpAsync(profile, eventId, cancellationToken);
        return Ok();
    }

    private Task<AuthenticatedUserProfile> CurrentProfile(CancellationToken cancellationToken)
    {
        return _profileResolver.ResolveCurrentAsync(User, cancellationToken);
    }
}
