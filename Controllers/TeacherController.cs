using Foundation.SecureLinks.Abstractions;
using Foundation.SecureLinks.Models;
using Foundation.SecureLinks.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using YourRhythmStudio.Application.Learning;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain;
using YourRhythmStudio.Domain.Learning.Enums;
using YourRhythmStudio.ViewModels.Learning;

namespace YourRhythmStudio.Controllers;

[Authorize(AuthenticationSchemes = "YourRhythmCookie", Roles = YourRhythmRoles.Teacher)]
[Route("Teacher")]
public sealed class TeacherController : Controller
{
    private readonly ILogger<TeacherController> _logger;
    private readonly IUserProfileResolver _profileResolver;
    private readonly TeacherStudentService _teacherStudentService;
    private readonly LessonService _lessonService;
    private readonly RepertoireService _repertoireService;
    private readonly AssignmentService _assignmentService;
    private readonly FeedbackService _feedbackService;
    private readonly SecureLinkService _secureLinkService;
    private readonly IQrArtifactGenerator _qrArtifactGenerator;
    private readonly SkillService _skillService;
    private readonly ProgressService _progressService;
    private readonly LevelConfigService _levelConfigService;
    private readonly MissionService _missionService;

    public TeacherController(
        ILogger<TeacherController> logger,
        IUserProfileResolver profileResolver,
        TeacherStudentService teacherStudentService,
        LessonService lessonService,
        RepertoireService repertoireService,
        AssignmentService assignmentService,
        FeedbackService feedbackService,
        SecureLinkService secureLinkService,
        IQrArtifactGenerator qrArtifactGenerator,
        SkillService skillService,
        ProgressService progressService,
        LevelConfigService levelConfigService,
        MissionService missionService)
    {
        _logger = logger;
        _profileResolver = profileResolver;
        _teacherStudentService = teacherStudentService;
        _lessonService = lessonService;
        _repertoireService = repertoireService;
        _assignmentService = assignmentService;
        _feedbackService = feedbackService;
        _secureLinkService = secureLinkService;
        _qrArtifactGenerator = qrArtifactGenerator;
        _skillService = skillService;
        _progressService = progressService;
        _levelConfigService = levelConfigService;
        _missionService = missionService;
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
        TeacherStudentSummary student;
        try
        {
            student = await _teacherStudentService.CreateStudentAsync(
                profile,
                new CreateTeacherStudentRequest(
                    model.DisplayName,
                    model.Contact,
                    model.Instrument,
                    model.Level,
                    model.Notes),
                cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            var message = ex is Microsoft.EntityFrameworkCore.DbUpdateException
                ? "Verifique os dados informados ou tente novamente."
                : ex.Message;
            ModelState.AddModelError(string.Empty, $"Nao foi possivel cadastrar o aluno: {message}");
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating a student for the current teacher.");
            ModelState.AddModelError(string.Empty, "Nao foi possivel cadastrar o aluno agora. Tente novamente em alguns instantes.");
            return View(model);
        }

        TempData["Success"] = $"Aluno {student.DisplayName} criado com sucesso.";
        return RedirectToAction(nameof(StudentDetail), new { studentId = student.StudentProfileId });
    }

    [HttpGet("Students/{studentId:guid}")]
    public async Task<IActionResult> StudentDetail(Guid studentId, CancellationToken cancellationToken)
    {
        var detail = await LoadStudentDetail(studentId, cancellationToken);
        if (detail is null) return Forbid();
        var profile = await CurrentProfile(cancellationToken);
        var skills = await _skillService.GetStudentSkillsAsync(profile, studentId, cancellationToken);
        var progress = await _progressService.GetTeacherStudentProgressAsync(profile, studentId, cancellationToken);
        var accessLink = await BuildStudentAccessLinkAsync(detail.Detail.Student, cancellationToken);
        return View(new TeacherStudentDetailWithSkillsViewModel { Base = detail, Skills = skills, Progress = progress, ActiveModule = "Summary", AccessLink = accessLink });
    }

    [HttpGet("Students/{studentId:guid}/Assignments")]
    public async Task<IActionResult> StudentAssignments(Guid studentId, CancellationToken cancellationToken)
        => await StudentModule(studentId, "Assignments", cancellationToken);

    [HttpGet("Students/{studentId:guid}/Repertoire")]
    public async Task<IActionResult> StudentRepertoire(Guid studentId, CancellationToken cancellationToken)
        => await StudentModule(studentId, "Repertoire", cancellationToken);

    [HttpGet("Students/{studentId:guid}/Feedback")]
    public async Task<IActionResult> StudentFeedback(Guid studentId, CancellationToken cancellationToken)
        => await StudentModule(studentId, "Feedback", cancellationToken);

    [HttpGet("Students/{studentId:guid}/Skills")]
    public async Task<IActionResult> StudentSkills(Guid studentId, CancellationToken cancellationToken)
        => await StudentModule(studentId, "Skills", cancellationToken);

    [HttpGet("Students/{studentId:guid}/Lessons")]
    public async Task<IActionResult> StudentLessons(Guid studentId, CancellationToken cancellationToken)
        => await StudentModule(studentId, "Lessons", cancellationToken);

    [HttpGet("Students/{studentId:guid}/Repertoire/{repertoireItemId:guid}/Audio")]
    public async Task<IActionResult> StudentRepertoireAudio(Guid studentId, Guid repertoireItemId, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var audio = await _repertoireService.GetAudioForTeacherAsync(profile, studentId, repertoireItemId, cancellationToken);
        if (audio is null) return NotFound();
        return PhysicalFile(audio.PhysicalPath, audio.ContentType, audio.DownloadFileName, enableRangeProcessing: true);
    }

    [HttpPost("Students/{studentId:guid}/Lessons")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLesson(
        Guid studentId,
        [Bind(Prefix = "Base.Lesson")] CreateLessonViewModel model,
        CancellationToken cancellationToken)
    {
        model = ReadLessonForm(model);
        TryValidateModel(model);
        if (!ModelState.IsValid || model.StudentProfileId != studentId)
        {
            TempData["Error"] = "Dados da aula invalidos.";
            return RedirectToAction(nameof(StudentDetail), new { studentId });
        }

        var profile = await CurrentProfile(cancellationToken);
        await _lessonService.CreateLessonAsync(
            profile,
            new CreateLessonRequest(studentId, BuildLessonTitle(model.Title, model.LessonDateUtc), model.LessonDateUtc, model.Notes),
            cancellationToken);

        return RedirectToAction(nameof(StudentLessons), new { studentId });
    }

    [HttpPost("Students/{studentId:guid}/Lessons/{lessonId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLesson(Guid studentId, Guid lessonId, EditLessonViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.StudentProfileId != studentId || model.LessonId != lessonId)
        {
            TempData["Error"] = "Dados da aula invalidos.";
            return RedirectToAction(nameof(StudentLessons), new { studentId });
        }

        var profile = await CurrentProfile(cancellationToken);
        try
        {
            await _lessonService.UpdateLessonAsync(
                profile,
                new UpdateLessonRequest(studentId, lessonId, BuildLessonTitle(model.Title, model.LessonDateUtc), model.LessonDateUtc, model.Notes),
                cancellationToken);
            TempData["Success"] = "Aula atualizada.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(StudentLessons), new { studentId });
    }

    [HttpGet("Students/{studentId:guid}/Lessons/{lessonId:guid}")]
    public async Task<IActionResult> LessonDetail(Guid studentId, Guid lessonId, CancellationToken cancellationToken)
    {
        try
        {
            var profile = await CurrentProfile(cancellationToken);
            var detail = await _lessonService.GetLessonDetailAsync(profile, studentId, lessonId, cancellationToken);
            return View(new TeacherLessonDetailViewModel { Detail = detail });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("Students/{studentId:guid}/Repertoire")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRepertoire(
        Guid studentId,
        [Bind(Prefix = "Base.Repertoire")] AddRepertoireViewModel model,
        CancellationToken cancellationToken)
    {
        model = ReadRepertoireForm(model);
        TryValidateModel(model);
        if (!ModelState.IsValid || model.StudentProfileId != studentId)
        {
            TempData["Error"] = "Dados do repertorio invalidos.";
            return RedirectToAction(nameof(StudentDetail), new { studentId });
        }

        var profile = await CurrentProfile(cancellationToken);
        try
        {
            var audio = model.AudioFile is null
                ? null
                : new RepertoireAudioUpload(
                    model.AudioFile.FileName,
                    model.AudioFile.ContentType,
                    model.AudioFile.Length,
                    model.AudioFile.OpenReadStream);

            await _repertoireService.AddRepertoireAsync(
                profile,
                new AddRepertoireRequest(
                    studentId,
                    model.Title,
                    model.Notes,
                    model.ReferenceUrl,
                    audio),
                cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["Error"] = $"Nao foi possivel adicionar o repertorio: {ex.Message}";
            return RedirectToAction(nameof(StudentDetail), new { studentId });
        }

        return RedirectToAction(nameof(StudentRepertoire), new { studentId });
    }

    [HttpPost("Students/{studentId:guid}/Repertoire/{repertoireItemId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRepertoire(
        Guid studentId,
        Guid repertoireItemId,
        EditRepertoireViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.StudentProfileId != studentId || model.RepertoireItemId != repertoireItemId)
        {
            TempData["Error"] = "Dados do repertorio invalidos.";
            return RedirectToAction(nameof(StudentRepertoire), new { studentId });
        }

        var profile = await CurrentProfile(cancellationToken);
        try
        {
            var audio = model.AudioFile is null
                ? null
                : new RepertoireAudioUpload(
                    model.AudioFile.FileName,
                    model.AudioFile.ContentType,
                    model.AudioFile.Length,
                    model.AudioFile.OpenReadStream);

            await _repertoireService.UpdateRepertoireAsync(
                profile,
                new UpdateRepertoireRequest(studentId, repertoireItemId, model.Title, model.Notes, model.ReferenceUrl, audio),
                cancellationToken);
            TempData["Success"] = "Repertorio atualizado.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(StudentRepertoire), new { studentId });
    }

    [HttpPost("Students/{studentId:guid}/Assignments")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAssignment(
        Guid studentId,
        [Bind(Prefix = "Base.Assignment")] CreateAssignmentViewModel model,
        CancellationToken cancellationToken)
    {
        await CurrentProfile(cancellationToken);
        TempData["Error"] = "Use a central de missoes para criar atividades pedagogicas.";
        return RedirectToAction(nameof(CreateMission), new { studentId });
    }

    [HttpPost("Students/{studentId:guid}/Assignments/{assignmentId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAssignment(Guid studentId, Guid assignmentId, EditAssignmentViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.StudentProfileId != studentId || model.AssignmentId != assignmentId)
        {
            TempData["Error"] = "Dados da missao invalidos.";
            return RedirectToAction(nameof(StudentAssignments), new { studentId });
        }

        var profile = await CurrentProfile(cancellationToken);
        try
        {
            await _assignmentService.UpdateAssignmentAsync(
                profile,
                new UpdateAssignmentRequest(
                    studentId,
                    assignmentId,
                    model.Title,
                    string.IsNullOrWhiteSpace(model.Description) ? model.Title : model.Description,
                    model.DueAtUtc,
                    model.XpReward,
                    model.Rarity,
                    model.SkillRewardId),
                cancellationToken);
            TempData["Success"] = "Missao atualizada.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(StudentAssignments), new { studentId });
    }

    [HttpPost("Students/{studentId:guid}/Feedback")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFeedback(
        Guid studentId,
        [Bind(Prefix = "Base.Feedback")] CreateFeedbackViewModel model,
        CancellationToken cancellationToken)
    {
        model = ReadFeedbackForm(model);
        TryValidateModel(model);
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

        return RedirectToAction(nameof(StudentFeedback), new { studentId });
    }

    [HttpPost("Students/{studentId:guid}/Feedback/{feedbackId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFeedback(Guid studentId, Guid feedbackId, EditFeedbackViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.StudentProfileId != studentId || model.FeedbackId != feedbackId)
        {
            TempData["Error"] = "Dados do feedback invalidos.";
            return RedirectToAction(nameof(StudentFeedback), new { studentId });
        }

        var profile = await CurrentProfile(cancellationToken);
        try
        {
            await _feedbackService.UpdateFeedbackAsync(
                profile,
                new UpdateFeedbackRequest(studentId, feedbackId, model.Message, model.VisibleToStudent),
                cancellationToken);
            TempData["Success"] = "Feedback atualizado.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(StudentFeedback), new { studentId });
    }

    [HttpPost("Students/{studentId:guid}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStudent(
        Guid studentId,
        [Bind(Prefix = "Base.EditStudent")] EditStudentViewModel model,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.StudentProfileId != studentId)
        {
            TempData["Error"] = "Dados do aluno invalidos.";
            return RedirectToStudentContext(studentId, returnUrl);
        }

        var profile = await CurrentProfile(cancellationToken);
        try
        {
            await _teacherStudentService.UpdateStudentAsync(
                profile,
                new UpdateTeacherStudentRequest(studentId, model.DisplayName, model.Instrument, model.Level, model.Notes),
                cancellationToken);
            TempData["Success"] = "Aluno atualizado.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToStudentContext(studentId, returnUrl);
    }

    [HttpPost("Students/{studentId:guid}/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStudent(Guid studentId, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        try
        {
            await _teacherStudentService.DeactivateStudentAsync(profile, studentId, cancellationToken);
            TempData["Success"] = "Aluno removido da lista ativa. O historico pedagogico foi preservado.";
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deactivating student {StudentProfileId}.", studentId);
            TempData["Error"] = "Nao foi possivel apagar o aluno agora. Tente novamente em alguns instantes.";
        }

        return RedirectToAction(nameof(Students));
    }

    [HttpPost("Students/{studentId:guid}/AccessLink")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateStudentAccessLink(Guid studentId, CancellationToken cancellationToken)
    {
        var detail = await LoadStudentDetail(studentId, cancellationToken);
        if (detail is null)
            return Forbid();

        var baseUri = new Uri($"{Request.Scheme}://{Request.Host}");
        var result = await _secureLinkService.CreateAsync(
            baseUri,
            new CreateSecureLinkRequest(
                Label: $"Acesso aluno {detail.Detail.Student.DisplayName}",
                ResourceKey: studentId.ToString(),
                RelativePath: "/Auth/StudentAccess"),
            cancellationToken);

        if (result.IsFailure)
        {
            TempData["Error"] = "Nao foi possivel gerar o link.";
            return RedirectToAction(nameof(StudentDetail), new { studentId });
        }

        var issued = result.Value!;
        var accessUrl = QueryHelpers.AddQueryString(issued.AbsoluteUrl, "code", issued.PublicCode);
        TempData["StudentAccessLink"] = accessUrl;
        return RedirectToAction(nameof(StudentDetail), new { studentId });
    }

    [HttpGet("Skills")]
    public async Task<IActionResult> Skills(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var skills = await _skillService.ListSkillsAsync(profile, cancellationToken);
        return View(new TeacherSkillsViewModel { Skills = skills });
    }

    [HttpPost("Skills")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSkill([Bind(Prefix = "NewSkill")] DefineSkillViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dados inválidos. Verifique os campos e tente novamente.";
            return RedirectToAction(nameof(Skills));
        }
        var profile = await CurrentProfile(cancellationToken);
        await _skillService.CreateSkillAsync(profile, new CreateSkillRequest(
            model.Name, model.Description, model.RequiredLevel, model.SkillType, model.IconName,
            model.AchievementText, model.ConquestCriteria), cancellationToken);
        TempData["Success"] = "Skill criada com sucesso.";
        return RedirectToAction(nameof(Skills));
    }

    [HttpPost("Skills/{skillId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSkill(Guid skillId, EditSkillViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.SkillId != skillId)
        {
            TempData["Error"] = "Dados inválidos. Verifique os campos e tente novamente.";
            return RedirectToAction(nameof(Skills));
        }
        var profile = await CurrentProfile(cancellationToken);
        try
        {
            await _skillService.UpdateSkillAsync(profile, new UpdateSkillRequest(
                skillId, model.Name, model.Description, model.RequiredLevel, model.SkillType,
                model.IconName, model.AchievementText, model.ConquestCriteria), cancellationToken);
            TempData["Success"] = "Skill atualizada.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Skills));
    }

    [HttpPost("Students/{studentId:guid}/Skills")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStudentSkill(Guid studentId, DefineSkillViewModel model, CancellationToken cancellationToken)
    {
        var detail = await LoadStudentDetail(studentId, cancellationToken);
        if (detail is null) return Forbid();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dados da skill invalidos.";
            return RedirectToAction(nameof(StudentSkills), new { studentId });
        }

        var profile = await CurrentProfile(cancellationToken);
        await _skillService.CreateSkillAsync(profile, new CreateSkillRequest(
            model.Name,
            model.Description,
            model.RequiredLevel,
            model.SkillType,
            model.IconName,
            model.AchievementText,
            model.ConquestCriteria), cancellationToken);

        TempData["Success"] = "Skill criada.";
        return RedirectToAction(nameof(StudentSkills), new { studentId });
    }

    [HttpPost("Students/{studentId:guid}/Skills/{skillId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStudentSkill(Guid studentId, Guid skillId, EditSkillViewModel model, CancellationToken cancellationToken)
    {
        var detail = await LoadStudentDetail(studentId, cancellationToken);
        if (detail is null) return Forbid();

        if (!ModelState.IsValid || model.SkillId != skillId)
        {
            TempData["Error"] = "Dados da skill invalidos.";
            return RedirectToAction(nameof(StudentSkills), new { studentId });
        }

        var profile = await CurrentProfile(cancellationToken);
        try
        {
            await _skillService.UpdateSkillAsync(profile, new UpdateSkillRequest(
                skillId,
                model.Name,
                model.Description,
                model.RequiredLevel,
                model.SkillType,
                model.IconName,
                model.AchievementText,
                model.ConquestCriteria), cancellationToken);
            TempData["Success"] = "Skill atualizada.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(StudentSkills), new { studentId });
    }

    [HttpPost("Skills/{skillId:guid}/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSkill(Guid skillId, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        await _skillService.DeleteSkillAsync(profile, skillId, cancellationToken);
        return RedirectToAction(nameof(Skills));
    }

    [HttpPost("Students/{studentId:guid}/Skills/{skillId:guid}/Toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSkillMastery(Guid studentId, Guid skillId, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        await _skillService.ToggleMasteryAsync(profile, studentId, skillId, cancellationToken);
        return RedirectToAction(nameof(StudentSkills), new { studentId });
    }

    [HttpGet("Levels")]
    public async Task<IActionResult> Levels(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var levelConfigs = await _levelConfigService.GetAllForTeacherAsync(profile, cancellationToken);
        var skills = await _skillService.ListSkillsAsync(profile, cancellationToken);
        return View(new TeacherLevelsViewModel { LevelConfigs = levelConfigs, Skills = skills });
    }

    [HttpGet("Levels/{level:int}")]
    public async Task<IActionResult> LevelDetail(int level, CancellationToken cancellationToken)
    {
        if (level is < 1 or > 5) return NotFound();
        var profile = await CurrentProfile(cancellationToken);
        var config = await _levelConfigService.GetForLevelAsync(profile, level, cancellationToken);
        var skills = await _skillService.ListSkillsAsync(profile, cancellationToken);
        return View(new TeacherLevelDetailViewModel
        {
            Config = config,
            Skills = skills.Where(s => s.RequiredLevel == level).ToArray(),
            Form = new SaveLevelConfigViewModel
            {
                Level = config.Level,
                Subtitle = config.Subtitle,
                Description = config.Description,
                TeacherExpectations = config.TeacherExpectations,
                Objectives = config.Objectives,
                ConquestMessage = config.ConquestMessage,
                OrientationMessage = config.OrientationMessage,
            }
        });
    }

    [HttpPost("Levels/{level:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLevelConfig(int level, [Bind(Prefix = "Form")] SaveLevelConfigViewModel model, CancellationToken cancellationToken)
    {
        if (level is < 1 or > 5) return NotFound();
        if (!ModelState.IsValid)
        {
            var profile2 = await CurrentProfile(cancellationToken);
            var config2  = await _levelConfigService.GetForLevelAsync(profile2, level, cancellationToken);
            var skills2  = await _skillService.ListSkillsAsync(profile2, cancellationToken);
            return View(nameof(LevelDetail), new TeacherLevelDetailViewModel
            {
                Config = config2,
                Skills = skills2.Where(s => s.RequiredLevel == level).ToArray(),
                Form   = model
            });
        }
        var profile = await CurrentProfile(cancellationToken);
        try
        {
            await _levelConfigService.UpsertAsync(profile, new UpdateLevelConfigRequest(
                level,
                model.Subtitle,
                model.Description,
                model.TeacherExpectations,
                model.Objectives,
                model.ConquestMessage,
                model.OrientationMessage), cancellationToken);
            TempData["Success"] = "Configuração salva com sucesso.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(LevelDetail), new { level });
    }

    // ── Missions ──────────────────────────────────────────────────────────────

    [HttpGet("Missions")]
    public async Task<IActionResult> Missions(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var missions = await _missionService.ListForTeacherAsync(profile, cancellationToken);
        var pending = await _missionService.ListAwaitingReviewAsync(profile, cancellationToken);
        return View("Devolutivas", new DevolutivasViewModel { Missions = missions, Pending = pending });
    }

    [HttpGet("Devolutivas")]
    public IActionResult Devolutivas()
        => RedirectToAction(nameof(Missions));

    [HttpGet("Missions/Create")]
    [HttpGet("Students/{studentId:guid}/Missions/Create")]
    public async Task<IActionResult> CreateMission(Guid? studentId, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var students = await _teacherStudentService.ListStudentsAsync(profile, cancellationToken);
        var selectedStudent = studentId.HasValue
            ? students.FirstOrDefault(student => student.StudentProfileId == studentId.Value)
            : students.FirstOrDefault();

        if (studentId.HasValue && selectedStudent is null) return Forbid();

        var model = new CreateMissionViewModel
        {
            StudentProfileId = selectedStudent?.StudentProfileId ?? Guid.Empty,
            StudentName = selectedStudent?.DisplayName ?? string.Empty,
            StudentOptions = students
        };

        await PopulateMissionCreateLookups(profile, model, cancellationToken);
        return View(model);
    }

    [HttpPost("Missions/Create")]
    [HttpPost("Students/{studentId:guid}/Missions/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMission(
        Guid? studentId,
        CreateMissionViewModel model,
        CancellationToken cancellationToken)
    {
        if (studentId.HasValue && model.StudentProfileId != studentId.Value)
        {
            ModelState.AddModelError(nameof(model.StudentProfileId), "Aluno invalido para esta missao.");
        }

        var profile = await CurrentProfile(cancellationToken);
        await PopulateMissionCreateLookups(profile, model, cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        List<CreateMissionQuestionRequest> questions;
        try
        {
            questions = System.Text.Json.JsonSerializer.Deserialize<List<CreateMissionQuestionRequest>>(
                model.QuestionsJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<CreateMissionQuestionRequest>();
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Perguntas invalidas. Tente novamente.");
            await PopulateMissionCreateLookups(profile, model, cancellationToken);
            return View(model);
        }

        if (questions.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Adicione ao menos uma pergunta a missao.");
            await PopulateMissionCreateLookups(profile, model, cancellationToken);
            return View(model);
        }

        try
        {
            var dueAtUtc = model.DueAtLocal.HasValue
                ? model.DueAtLocal.Value.Kind == DateTimeKind.Utc
                    ? model.DueAtLocal.Value
                    : model.DueAtLocal.Value.ToUniversalTime()
                : (DateTime?)null;

            var missionId = await _missionService.CreateMissionAsync(
                profile,
                new CreateMissionRequest(
                    model.StudentProfileId,
                    model.Title,
                    model.Description,
                    dueAtUtc,
                    model.XpReward,
                    model.Rarity,
                    model.SkillRewardId,
                    questions),
                cancellationToken);

            TempData["Success"] = "Missao criada com sucesso.";
            return RedirectToAction(nameof(MissionDetail),
                new { studentId = model.StudentProfileId, missionId });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateMissionCreateLookups(profile, model, cancellationToken);
            return View(model);
        }
    }

    // ── Mission detail (teacher view) ─────────────────────────────────────────

    [HttpGet("Students/{studentId:guid}/Missions/{missionId:guid}")]
    public async Task<IActionResult> MissionDetail(
        Guid studentId,
        Guid missionId,
        CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var detail = await _missionService.GetMissionDetailForTeacherAsync(profile, missionId, cancellationToken);
        if (detail is null || detail.Mission.StudentProfileId != studentId)
            return NotFound();

        return View(new TeacherMissionDetailViewModel { Detail = detail });
    }

    [HttpPost("Students/{studentId:guid}/Missions/{missionId:guid}/Review")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewMission(
        Guid studentId,
        Guid missionId,
        ReviewMissionViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.MissionId != missionId)
        {
            TempData["Error"] = "Dados invalidos.";
            return RedirectToAction(nameof(MissionDetail), new { studentId, missionId });
        }

        var profile = await CurrentProfile(cancellationToken);
        try
        {
            await _missionService.ReviewMissionAsync(
                profile, missionId, model.Decision, model.Feedback, cancellationToken);

            TempData["Success"] = model.Decision == MissionReviewDecision.Approved
                ? "Missao aprovada! XP concedido ao aluno."
                : "Ajustes solicitados com sucesso.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(MissionDetail), new { studentId, missionId });
    }

    [HttpGet("Missions/{missionId:guid}/Answers/{questionId:guid}/File")]
    public async Task<IActionResult> MissionAnswerFile(
        Guid missionId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var file = await _missionService.GetAnswerFileAsync(profile, missionId, questionId, cancellationToken);
        if (file is null) return NotFound();
        return PhysicalFile(file.Value.PhysicalPath, file.Value.ContentType, file.Value.FileName, enableRangeProcessing: true);
    }

    // ── Quick student search ──────────────────────────────────────────────────

    [HttpGet("Students/Search")]
    public async Task<IActionResult> SearchStudents(string? q, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var students = await _teacherStudentService.ListStudentsAsync(profile, cancellationToken);
        var term = q?.Trim() ?? string.Empty;
        var matches = students
            .Where(s => string.IsNullOrEmpty(term)
                     || s.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .Select(s => new { id = s.StudentProfileId, name = s.DisplayName, instrument = s.Instrument });
        return Ok(matches);
    }

    [HttpGet("QuickLesson")]
    public async Task<IActionResult> QuickLesson(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        var students = await _teacherStudentService.ListStudentsAsync(profile, cancellationToken);
        return View(new QuickLessonViewModel { Students = students });
    }

    [HttpPost("QuickLesson")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickLesson([Bind(Prefix = "Form")] QuickLessonFormViewModel model, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfile(cancellationToken);
        if (!ModelState.IsValid)
        {
            var students2 = await _teacherStudentService.ListStudentsAsync(profile, cancellationToken);
            return View(new QuickLessonViewModel { Students = students2, Form = model });
        }
        await _lessonService.CreateLessonAsync(
            profile,
            new CreateLessonRequest(
                model.StudentProfileId,
                BuildLessonTitle(model.Title, model.LessonDateUtc),
                model.LessonDateUtc.ToUniversalTime(),
                model.Notes),
            cancellationToken);
        return RedirectToAction(nameof(StudentDetail), new { studentId = model.StudentProfileId });
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
                Repertoire = new AddRepertoireViewModel { StudentProfileId = studentId },
                Assignment = new CreateAssignmentViewModel { StudentProfileId = studentId, XpReward = DefaultXpFor(AssignmentRarity.Comum) },
                Feedback = new CreateFeedbackViewModel { StudentProfileId = studentId, VisibleToStudent = true },
                EditStudent = new EditStudentViewModel
                {
                    StudentProfileId = studentId,
                    DisplayName = detail.Student.DisplayName,
                    Instrument = detail.Student.Instrument,
                    Level = detail.Student.Level,
                    Notes = detail.Student.Notes
                }
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

    private async Task<IActionResult> StudentModule(Guid studentId, string module, CancellationToken cancellationToken)
    {
        var detail = await LoadStudentDetail(studentId, cancellationToken);
        if (detail is null) return Forbid();

        var profile = await CurrentProfile(cancellationToken);
        var skills = await _skillService.GetStudentSkillsAsync(profile, studentId, cancellationToken);
        var progress = await _progressService.GetTeacherStudentProgressAsync(profile, studentId, cancellationToken);
        var accessLink = await BuildStudentAccessLinkAsync(detail.Detail.Student, cancellationToken);

        return View($"Student{module}", new TeacherStudentDetailWithSkillsViewModel
        {
            Base = detail,
            Skills = skills,
            Progress = progress,
            ActiveModule = module,
            AccessLink = accessLink
        });
    }

    private async Task<StudentAccessLinkViewModel?> BuildStudentAccessLinkAsync(TeacherStudentSummary student, CancellationToken cancellationToken)
    {
        var baseUri = new Uri($"{Request.Scheme}://{Request.Host}");
        var result = await _secureLinkService.CreateAsync(
            baseUri,
            new CreateSecureLinkRequest(
                Label: $"Acesso aluno {student.DisplayName}",
                ResourceKey: student.StudentProfileId.ToString(),
                RelativePath: "/Auth/StudentAccess"),
            cancellationToken);

        if (result.IsFailure || result.Value is null)
            return null;

        var issued = result.Value;
        var accessUrl = QueryHelpers.AddQueryString(issued.AbsoluteUrl, "code", issued.PublicCode);
        var qr = await _qrArtifactGenerator.GenerateSvgAsync(accessUrl, $"student-access-{student.StudentProfileId:N}", cancellationToken);
        return new StudentAccessLinkViewModel(accessUrl, qr.DataUri);
    }

    private async Task PopulateMissionCreateLookups(
        AuthenticatedUserProfile profile,
        CreateMissionViewModel model,
        CancellationToken cancellationToken)
    {
        var students = await _teacherStudentService.ListStudentsAsync(profile, cancellationToken);
        model.StudentOptions = students;
        model.StudentName = students
            .FirstOrDefault(student => student.StudentProfileId == model.StudentProfileId)
            ?.DisplayName ?? string.Empty;
        ViewBag.Skills = await _skillService.ListSkillsAsync(profile, cancellationToken);
    }

    private Task<AuthenticatedUserProfile> CurrentProfile(CancellationToken cancellationToken)
    {
        return _profileResolver.ResolveCurrentAsync(User, cancellationToken);
    }

    private IActionResult RedirectToStudentContext(Guid studentId, string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(StudentDetail), new { studentId });
    }

    private static string BuildLessonTitle(string? title, DateTime lessonDate)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            return title.Trim();
        }

        var local = lessonDate.Kind == DateTimeKind.Utc ? lessonDate.ToLocalTime() : lessonDate;
        return $"Aula de {local:dd/MM/yyyy} as {local:HH:mm}";
    }

    private static int DefaultXpFor(AssignmentRarity rarity)
        => LearningLevelCalculator.DefaultXpForRarity(rarity);

    private CreateLessonViewModel ReadLessonForm(CreateLessonViewModel current)
    {
        ModelState.Clear();
        return new CreateLessonViewModel
        {
            StudentProfileId = ReadGuid(current.StudentProfileId, "Base.Lesson.StudentProfileId", "Lesson.StudentProfileId"),
            Title = ReadString(current.Title, "Base.Lesson.Title", "Lesson.Title"),
            LessonDateUtc = ReadDateTime(current.LessonDateUtc, "Base.Lesson.LessonDateUtc", "Lesson.LessonDateUtc") ?? current.LessonDateUtc,
            Notes = ReadString(current.Notes, "Base.Lesson.Notes", "Lesson.Notes")
        };
    }

    private AddRepertoireViewModel ReadRepertoireForm(AddRepertoireViewModel current)
    {
        ModelState.Clear();
        return new AddRepertoireViewModel
        {
            StudentProfileId = ReadGuid(current.StudentProfileId, "Base.Repertoire.StudentProfileId", "Repertoire.StudentProfileId"),
            Title = ReadString(current.Title, "Base.Repertoire.Title", "Repertoire.Title"),
            Notes = ReadString(current.Notes, "Base.Repertoire.Notes", "Repertoire.Notes"),
            ReferenceUrl = ReadString(current.ReferenceUrl, "Base.Repertoire.ReferenceUrl", "Repertoire.ReferenceUrl"),
            AudioFile = Request.Form.Files.GetFile("Base.Repertoire.AudioFile") ?? Request.Form.Files.GetFile("Repertoire.AudioFile")
        };
    }

    private CreateAssignmentViewModel ReadAssignmentForm(CreateAssignmentViewModel current)
    {
        ModelState.Clear();
        return new CreateAssignmentViewModel
        {
            StudentProfileId = ReadGuid(current.StudentProfileId, "Base.Assignment.StudentProfileId", "Assignment.StudentProfileId"),
            Title = ReadString(current.Title, "Base.Assignment.Title", "Assignment.Title"),
            Description = ReadString(current.Description, "Base.Assignment.Description", "Assignment.Description"),
            DueAtUtc = ReadDateTime(current.DueAtUtc, "Base.Assignment.DueAtUtc", "Assignment.DueAtUtc"),
            XpReward = ReadInt(current.XpReward, "Base.Assignment.XpReward", "Assignment.XpReward"),
            Rarity = ReadEnum(current.Rarity, "Base.Assignment.Rarity", "Assignment.Rarity"),
            UseDefaultXp = ReadBool(current.UseDefaultXp, "Base.Assignment.UseDefaultXp", "Assignment.UseDefaultXp"),
        };
    }

    private CreateFeedbackViewModel ReadFeedbackForm(CreateFeedbackViewModel current)
    {
        ModelState.Clear();
        return new CreateFeedbackViewModel
        {
            StudentProfileId = ReadGuid(current.StudentProfileId, "Base.Feedback.StudentProfileId", "Feedback.StudentProfileId"),
            Message = ReadString(current.Message, "Base.Feedback.Message", "Feedback.Message"),
            VisibleToStudent = ReadBool(current.VisibleToStudent, "Base.Feedback.VisibleToStudent", "Feedback.VisibleToStudent")
        };
    }

    private string ReadString(string? fallback, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (Request.Form.TryGetValue(key, out var value))
                return value.ToString().Trim();
        }

        return fallback?.Trim() ?? string.Empty;
    }

    private Guid ReadGuid(Guid fallback, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (Request.Form.TryGetValue(key, out var value) && Guid.TryParse(value.ToString(), out var parsed))
                return parsed;
        }

        return fallback;
    }

    private int ReadInt(int fallback, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (Request.Form.TryGetValue(key, out var value) && int.TryParse(value.ToString(), out var parsed))
                return parsed;
        }

        return fallback;
    }

    private DateTime? ReadDateTime(DateTime? fallback, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!Request.Form.TryGetValue(key, out var value))
                continue;

            var raw = value.ToString();
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            if (DateTime.TryParse(raw, out var parsed))
                return parsed;
        }

        return fallback;
    }

    private TEnum ReadEnum<TEnum>(TEnum fallback, params string[] keys) where TEnum : struct, Enum
    {
        foreach (var key in keys)
        {
            if (Request.Form.TryGetValue(key, out var value) && Enum.TryParse<TEnum>(value.ToString(), out var parsed))
                return parsed;
        }
        return fallback;
    }

    private bool ReadBool(bool fallback, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!Request.Form.TryGetValue(key, out var value))
                continue;

            var raw = value.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (bool.TryParse(raw, out var parsed))
                return parsed;
        }

        return fallback;
    }
}
