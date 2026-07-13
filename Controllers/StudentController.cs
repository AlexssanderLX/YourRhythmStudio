using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourRhythmStudio.Application.Learning;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain;
using YourRhythmStudio.Domain.Learning.Enums;
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
    private readonly MissionService _missionService;

    public StudentController(
        IUserProfileResolver profileResolver,
        AssignmentService assignmentService,
        RepertoireService repertoireService,
        FeedbackService feedbackService,
        ProgressService progressService,
        SkillService skillService,
        LevelConfigService levelConfigService,
        MissionService missionService)
    {
        _profileResolver = profileResolver;
        _assignmentService = assignmentService;
        _repertoireService = repertoireService;
        _feedbackService = feedbackService;
        _progressService = progressService;
        _skillService = skillService;
        _levelConfigService = levelConfigService;
        _missionService = missionService;
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

    // ── Missions ──────────────────────────────────────────────────────────────

    [HttpGet("Missions")]
    public async Task<IActionResult> Missions(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var missions = await _missionService.ListForCurrentStudentAsync(profile, cancellationToken);
        return View(new StudentMissionsViewModel { Missions = missions });
    }

    [HttpGet("Missions/{missionId:guid}")]
    public async Task<IActionResult> MissionDetail(Guid missionId, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var detail = await _missionService.GetMissionDetailForStudentAsync(profile, missionId, cancellationToken);
        if (detail is null) return NotFound();

        return View(new StudentMissionDetailViewModel { Detail = detail });
    }

    [HttpPost("Missions/{missionId:guid}/Save")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> SaveMissionDraft(Guid missionId, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var answers = ParseAnswersFromForm(missionId);

        try
        {
            await _missionService.SaveAnswersAsync(profile, missionId, answers, submit: false, cancellationToken);
            TempData["Success"] = "Rascunho salvo.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(MissionDetail), new { missionId });
    }

    [HttpPost("Missions/{missionId:guid}/Submit")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> SubmitMission(Guid missionId, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var answers = ParseAnswersFromForm(missionId);

        try
        {
            await _missionService.SaveAnswersAsync(profile, missionId, answers, submit: true, cancellationToken);
            TempData["Success"] = "Missao enviada para revisao do professor!";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(MissionDetail), new { missionId });
        }

        return RedirectToAction(nameof(Missions));
    }

    [HttpGet("Missions/{missionId:guid}/Answers/{questionId:guid}/File")]
    public async Task<IActionResult> MissionAnswerFile(
        Guid missionId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var file = await _missionService.GetStudentAnswerFileAsync(profile, missionId, questionId, cancellationToken);
        if (file is null) return NotFound();
        return PhysicalFile(file.Value.PhysicalPath, file.Value.ContentType, file.Value.FileName, enableRangeProcessing: true);
    }

    private List<(Guid QuestionId, string? Text, Microsoft.AspNetCore.Http.IFormFile? File)> ParseAnswersFromForm(Guid missionId)
    {
        var result = new List<(Guid, string?, Microsoft.AspNetCore.Http.IFormFile?)>();

        foreach (var key in Request.Form.Keys)
        {
            if (!key.StartsWith("text_", StringComparison.OrdinalIgnoreCase)) continue;
            if (!Guid.TryParse(key["text_".Length..], out var qId)) continue;
            var text = Request.Form[key].ToString();
            var file = Request.Form.Files.GetFile($"file_{qId}");
            result.Add((qId, string.IsNullOrWhiteSpace(text) ? null : text, file));
        }

        foreach (var formFile in Request.Form.Files)
        {
            if (!formFile.Name.StartsWith("file_", StringComparison.OrdinalIgnoreCase)) continue;
            if (!Guid.TryParse(formFile.Name["file_".Length..], out var qId)) continue;
            if (result.Any(r => r.Item1 == qId)) continue;
            result.Add((qId, null, formFile));
        }

        return result;
    }

    [HttpGet("Levels")]
    public async Task<IActionResult> Levels(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var progress     = await _progressService.GetCurrentStudentProgressAsync(profile, cancellationToken);
        var skills       = await _skillService.GetStudentSkillsForStudentAsync(profile, cancellationToken);
        var levelConfigs = await _levelConfigService.GetAllForStudentAsync(profile, cancellationToken);
        return View(new StudentLevelsViewModel
        {
            Progress = progress,
            Skills = skills,
            LevelConfigs = levelConfigs
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
