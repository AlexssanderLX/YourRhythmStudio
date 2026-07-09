using Foundation.SecureLinks.Models;
using YourRhythmStudio.Domain.Learning.Enums;
using Foundation.SecureLinks.Services;
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
    private readonly SecureLinkService _secureLinkService;
    private readonly SkillService _skillService;

    public TeacherController(
        IUserProfileResolver profileResolver,
        TeacherStudentService teacherStudentService,
        LessonService lessonService,
        RepertoireService repertoireService,
        AssignmentService assignmentService,
        FeedbackService feedbackService,
        SecureLinkService secureLinkService,
        SkillService skillService)
    {
        _profileResolver = profileResolver;
        _teacherStudentService = teacherStudentService;
        _lessonService = lessonService;
        _repertoireService = repertoireService;
        _assignmentService = assignmentService;
        _feedbackService = feedbackService;
        _secureLinkService = secureLinkService;
        _skillService = skillService;
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
                    model.Email,
                    model.Instrument,
                    model.Level,
                    model.Notes),
                cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            var message = ex is Microsoft.EntityFrameworkCore.DbUpdateException
                ? "Verifique se o e-mail ja esta cadastrado ou tente novamente."
                : ex.Message;
            ModelState.AddModelError(string.Empty, $"Nao foi possivel cadastrar o aluno: {message}");
            return View(model);
        }

        return RedirectToAction(nameof(StudentDetail), new { studentId = student.StudentProfileId });
    }

    [HttpGet("Students/{studentId:guid}")]
    public async Task<IActionResult> StudentDetail(Guid studentId, CancellationToken cancellationToken)
    {
        var detail = await LoadStudentDetail(studentId, cancellationToken);
        if (detail is null) return Forbid();
        var profile = await CurrentProfile(cancellationToken);
        var skills = await _skillService.GetStudentSkillsAsync(profile, studentId, cancellationToken);
        return View(new TeacherStudentDetailWithSkillsViewModel { Base = detail, Skills = skills });
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
            new CreateLessonRequest(studentId, model.Title, model.LessonDateUtc, model.DurationMinutes, model.Notes),
            cancellationToken);

        return RedirectToAction(nameof(StudentDetail), new { studentId });
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
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["Error"] = $"Nao foi possivel adicionar o repertorio: {ex.Message}";
            return RedirectToAction(nameof(StudentDetail), new { studentId });
        }

        return RedirectToAction(nameof(StudentDetail), new { studentId });
    }

    [HttpPost("Students/{studentId:guid}/Assignments")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAssignment(
        Guid studentId,
        [Bind(Prefix = "Base.Assignment")] CreateAssignmentViewModel model,
        CancellationToken cancellationToken)
    {
        model = ReadAssignmentForm(model);
        TryValidateModel(model);
        if (!ModelState.IsValid || model.StudentProfileId != studentId)
        {
            TempData["Error"] = "Dados da missao invalidos.";
            return RedirectToAction(nameof(StudentDetail), new { studentId });
        }

        var profile = await CurrentProfile(cancellationToken);
        try
        {
            await _assignmentService.CreateAssignmentAsync(
                profile,
                new CreateAssignmentRequest(
                    studentId,
                    model.Title,
                    string.IsNullOrWhiteSpace(model.Description) ? model.Title : model.Description,
                    model.DueAtUtc,
                    model.TargetMinutes,
                    model.XpReward,
                    model.Rarity),
                cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["Error"] = $"Nao foi possivel criar a missao: {ex.Message}";
            return RedirectToAction(nameof(StudentDetail), new { studentId });
        }

        return RedirectToAction(nameof(StudentDetail), new { studentId });
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

        return RedirectToAction(nameof(StudentDetail), new { studentId });
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
        var accessUrl = $"{Request.Scheme}://{Request.Host}/Auth/StudentAccess?code={issued.PublicCode}";
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
            var profile2 = await CurrentProfile(cancellationToken);
            var skills2 = await _skillService.ListSkillsAsync(profile2, cancellationToken);
            return View(new TeacherSkillsViewModel { Skills = skills2, NewSkill = model });
        }
        var profile = await CurrentProfile(cancellationToken);
        await _skillService.CreateSkillAsync(profile, model.Name, model.Description, model.RequiredLevel, model.SkillType, model.IconName, cancellationToken);
        return RedirectToAction(nameof(Skills));
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
        return RedirectToAction(nameof(StudentDetail), new { studentId });
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
        var lessonTitle = string.IsNullOrWhiteSpace(model.Title)
            ? $"Aula - {model.LessonDateUtc:dd/MM/yyyy HH:mm}"
            : model.Title.Trim();

        await _lessonService.CreateLessonAsync(
            profile,
            new CreateLessonRequest(model.StudentProfileId, lessonTitle, model.LessonDateUtc.ToUniversalTime(), model.DurationMinutes, model.Notes),
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
                Lesson = new CreateLessonViewModel { StudentProfileId = studentId, LessonDateUtc = DateTime.UtcNow, DurationMinutes = 60 },
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

    private CreateLessonViewModel ReadLessonForm(CreateLessonViewModel current)
    {
        ModelState.Clear();
        return new CreateLessonViewModel
        {
            StudentProfileId = ReadGuid(current.StudentProfileId, "Base.Lesson.StudentProfileId", "Lesson.StudentProfileId"),
            Title = ReadString(current.Title, "Base.Lesson.Title", "Lesson.Title"),
            LessonDateUtc = ReadDateTime(current.LessonDateUtc, "Base.Lesson.LessonDateUtc", "Lesson.LessonDateUtc") ?? current.LessonDateUtc,
            DurationMinutes = ReadInt(current.DurationMinutes, "Base.Lesson.DurationMinutes", "Lesson.DurationMinutes"),
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
            ComposerOrArtist = ReadString(current.ComposerOrArtist, "Base.Repertoire.ComposerOrArtist", "Repertoire.ComposerOrArtist"),
            Instrument = ReadString(current.Instrument, "Base.Repertoire.Instrument", "Repertoire.Instrument"),
            Level = ReadString(current.Level, "Base.Repertoire.Level", "Repertoire.Level"),
            Notes = ReadString(current.Notes, "Base.Repertoire.Notes", "Repertoire.Notes"),
            ReferenceUrl = ReadString(current.ReferenceUrl, "Base.Repertoire.ReferenceUrl", "Repertoire.ReferenceUrl")
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
            TargetMinutes = ReadInt(current.TargetMinutes, "Base.Assignment.TargetMinutes", "Assignment.TargetMinutes"),
            XpReward = ReadInt(current.XpReward, "Base.Assignment.XpReward", "Assignment.XpReward"),
            Rarity = ReadEnum(current.Rarity, "Base.Assignment.Rarity", "Assignment.Rarity"),
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
