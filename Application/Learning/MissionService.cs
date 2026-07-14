using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using YourRhythmStudio.Application.Users;
using YourRhythmStudio.Domain.Learning;
using YourRhythmStudio.Domain.Learning.Enums;
using YourRhythmStudio.Domain.Users;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Application.Learning;

// ─── Request / Summary records ────────────────────────────────────────────────

public sealed record CreateMissionRequest(
    Guid StudentProfileId,
    string Title,
    string Description,
    DateTime? DueAtUtc,
    int XpReward,
    AssignmentRarity Rarity,
    Guid? SkillRewardId,
    IReadOnlyList<CreateMissionQuestionRequest> Questions);

public sealed record CreateMissionQuestionRequest(
    string QuestionText,
    MissionQuestionType QuestionType,
    int Order,
    bool IsRequired,
    IReadOnlyList<string>? Options = null);

public sealed record MissionSummary(
    Guid Id,
    Guid StudentProfileId,
    string StudentName,
    string Title,
    AssignmentStatus Status,
    int CurrentRound,
    DateTime? SubmittedForReviewAtUtc,
    DateTime CreatedAtUtc,
    int XpReward,
    AssignmentRarity Rarity);

public sealed record MissionQuestionDto(
    Guid Id,
    string QuestionText,
    MissionQuestionType QuestionType,
    int Order,
    bool IsRequired,
    IReadOnlyList<string> Options);

public sealed record MissionAnswerDto(
    Guid QuestionId,
    string? AnswerText,
    string? OriginalFileName,
    string? ContentType,
    long? SizeBytes,
    string? FileDownloadUrl);

public sealed record MissionReviewDto(
    Guid Id,
    MissionReviewDecision Decision,
    string? Feedback,
    int RoundNumber,
    DateTime ReviewedAtUtc);

public sealed record MissionDetailForTeacher(
    MissionSummary Mission,
    IReadOnlyList<MissionQuestionDto> Questions,
    IReadOnlyList<MissionAnswerDto> LatestAnswers,
    IReadOnlyList<MissionReviewDto> Reviews);

public sealed record MissionDetailForStudent(
    Guid Id,
    string Title,
    string Description,
    DateTime? DueAtUtc,
    AssignmentStatus Status,
    int CurrentRound,
    int XpReward,
    AssignmentRarity Rarity,
    DateTime CreatedAtUtc,
    IReadOnlyList<MissionQuestionDto> Questions,
    IReadOnlyList<MissionAnswerDto> CurrentAnswers,
    MissionReviewDto? LastReview);

// ─── Service ──────────────────────────────────────────────────────────────────

public sealed class MissionService
{
    private const long MaxAudioBytes = 10L * 1024 * 1024;   // 10 MB proxy for ≤5 min audio
    private const long MaxFileBytes  = 20L * 1024 * 1024;   // 20 MB for other files
    private const string UploadRoot  = "storage/uploads/mission-answers";

    private readonly YourRhythmDbContext _db;
    private readonly IWebHostEnvironment _env;

    public MissionService(YourRhythmDbContext db, IWebHostEnvironment env)
    {
        _db  = db;
        _env = env;
    }

    // ── Teacher: create mission ───────────────────────────────────────────────

    public async Task<Guid> CreateMissionAsync(
        AuthenticatedUserProfile profile,
        CreateMissionRequest request,
        CancellationToken ct = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _db, schoolId, teacherProfileId, request.StudentProfileId, ct);

        if (!request.Questions.Any())
            throw new ArgumentException("A missao precisa de pelo menos uma pergunta.");

        if (request.SkillRewardId.HasValue
            && request.Rarity is not (AssignmentRarity.Epica or AssignmentRarity.Lendaria))
        {
            throw new InvalidOperationException(
                "Recompensa de habilidade so pode ser vinculada a missoes Epicas ou Lendarias.");
        }

        if (request.SkillRewardId.HasValue)
        {
            var skillOk = await _db.Skills.AnyAsync(
                s => s.Id == request.SkillRewardId.Value
                    && s.SchoolId == schoolId
                    && s.TeacherProfileId == teacherProfileId
                    && s.IsActive, ct);
            if (!skillOk)
                throw new KeyNotFoundException("Habilidade nao encontrada.");
        }

        var now = DateTime.UtcNow;
        var mission = new Assignment(
            schoolId, teacherProfileId, request.StudentProfileId,
            request.Title, request.Description, request.DueAtUtc, request.XpReward, now,
            rarity: request.Rarity, skillRewardId: request.SkillRewardId, isMission: true);

        _db.Assignments.Add(mission);

        int order = 0;
        foreach (var q in request.Questions.OrderBy(x => x.Order))
        {
            var options = NormalizeQuestionOptions(q);
            _db.MissionQuestions.Add(new MissionQuestion(
                mission.Id,
                q.QuestionText,
                q.QuestionType,
                order++,
                q.IsRequired,
                options.Count == 0 ? null : JsonSerializer.Serialize(options)));
        }

        await _db.SaveChangesAsync(ct);
        return mission.Id;
    }

    // ── Teacher: list missions awaiting review (Devolutivas) ─────────────────

    public async Task<IReadOnlyList<MissionSummary>> ListAwaitingReviewAsync(
        AuthenticatedUserProfile profile,
        CancellationToken ct = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);

        var assignments = await _db.Assignments
            .AsNoTracking()
            .Where(a => a.SchoolId == schoolId
                     && a.TeacherProfileId == teacherProfileId
                     && a.IsMission
                     && a.Status == AssignmentStatus.AwaitingReview)
            .OrderBy(a => a.SubmittedForReviewAtUtc)
            .ToListAsync(ct);

        if (!assignments.Any()) return Array.Empty<MissionSummary>();

        var studentIds = assignments.Select(a => a.StudentProfileId).Distinct().ToList();
        var nameMap = await StudentNameMapAsync(studentIds, ct);

        return assignments.Select(a => new MissionSummary(
            a.Id, a.StudentProfileId,
            nameMap.GetValueOrDefault(a.StudentProfileId, "Aluno"),
            a.Title, a.Status, a.CurrentRound, a.SubmittedForReviewAtUtc,
            a.CreatedAtUtc, a.XpReward, a.Rarity)).ToList();
    }

    public async Task<IReadOnlyList<MissionSummary>> ListForTeacherAsync(
        AuthenticatedUserProfile profile,
        CancellationToken ct = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);

        var assignments = await _db.Assignments
            .AsNoTracking()
            .Where(a => a.SchoolId == schoolId
                     && a.TeacherProfileId == teacherProfileId
                     && a.IsMission
                     && a.Status != AssignmentStatus.Skipped
                     && _db.TeacherStudents.Any(link => link.SchoolId == schoolId
                         && link.TeacherProfileId == teacherProfileId
                         && link.StudentProfileId == a.StudentProfileId
                         && link.IsActive)
                     && _db.StudentProfiles.Any(student => student.SchoolId == schoolId
                         && student.Id == a.StudentProfileId
                         && student.SchoolUser != null
                         && student.SchoolUser.IsActive))
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(ct);

        if (!assignments.Any()) return Array.Empty<MissionSummary>();

        var studentIds = assignments.Select(a => a.StudentProfileId).Distinct().ToList();
        var nameMap = await StudentNameMapAsync(studentIds, ct);

        return assignments.Select(a => new MissionSummary(
            a.Id, a.StudentProfileId,
            nameMap.GetValueOrDefault(a.StudentProfileId, "Aluno"),
            a.Title, a.Status, a.CurrentRound, a.SubmittedForReviewAtUtc,
            a.CreatedAtUtc, a.XpReward, a.Rarity)).ToList();
    }

    // ── Teacher: list missions for a student ─────────────────────────────────

    public async Task CancelMissionAsync(
        AuthenticatedUserProfile profile,
        Guid missionId,
        CancellationToken ct = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);

        var assignment = await _db.Assignments
            .FirstOrDefaultAsync(a => a.Id == missionId
                                   && a.SchoolId == schoolId
                                   && a.TeacherProfileId == teacherProfileId
                                   && a.IsMission, ct)
            ?? throw new KeyNotFoundException("Missao nao encontrada.");

        assignment.ArchiveFromActiveHistory(DateTime.UtcNow);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<MissionSummary>> ListForTeacherStudentAsync(
        AuthenticatedUserProfile profile,
        Guid studentProfileId,
        CancellationToken ct = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);
        await LearningAuthorization.EnsureTeacherCanAccessStudentAsync(
            _db, schoolId, teacherProfileId, studentProfileId, ct);

        var nameMap = await StudentNameMapAsync(new[] { studentProfileId }, ct);
        var studentName = nameMap.GetValueOrDefault(studentProfileId, "Aluno");

        var assignments = await _db.Assignments
            .AsNoTracking()
            .Where(a => a.SchoolId == schoolId
                     && a.TeacherProfileId == teacherProfileId
                     && a.StudentProfileId == studentProfileId
                     && a.IsMission)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(ct);

        return assignments.Select(a => new MissionSummary(
            a.Id, a.StudentProfileId, studentName, a.Title, a.Status,
            a.CurrentRound, a.SubmittedForReviewAtUtc, a.CreatedAtUtc, a.XpReward, a.Rarity))
            .ToList();
    }

    // ── Teacher: get mission detail (with student answers + review history) ──

    public async Task<MissionDetailForTeacher?> GetMissionDetailForTeacherAsync(
        AuthenticatedUserProfile profile,
        Guid missionId,
        CancellationToken ct = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);

        var assignment = await _db.Assignments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == missionId
                                   && a.SchoolId == schoolId
                                   && a.TeacherProfileId == teacherProfileId
                                   && a.IsMission, ct);
        if (assignment is null) return null;

        var nameMap = await StudentNameMapAsync(new[] { assignment.StudentProfileId }, ct);
        var studentName = nameMap.GetValueOrDefault(assignment.StudentProfileId, "Aluno");

        var questions = await _db.MissionQuestions
            .AsNoTracking()
            .Where(q => q.AssignmentId == missionId)
            .OrderBy(q => q.Order)
            .Select(q => new MissionQuestionDto(
                q.Id,
                q.QuestionText,
                q.QuestionType,
                q.Order,
                q.IsRequired,
                ReadOptions(q.OptionsJson)))
            .ToListAsync(ct);

        var latestAnswers = await _db.MissionAnswers
            .AsNoTracking()
            .Where(a => a.AssignmentId == missionId && a.RoundNumber == assignment.CurrentRound)
            .Select(a => new MissionAnswerDto(
                a.QuestionId,
                a.AnswerText,
                a.OriginalFileName,
                a.ContentType,
                a.SizeBytes,
                a.StoredFileName != null
                    ? $"/Teacher/Missions/{missionId}/Answers/{a.QuestionId}/File"
                    : null))
            .ToListAsync(ct);

        var reviews = await _db.MissionReviews
            .AsNoTracking()
            .Where(r => r.AssignmentId == missionId)
            .OrderByDescending(r => r.ReviewedAtUtc)
            .Select(r => new MissionReviewDto(r.Id, r.Decision, r.Feedback, r.RoundNumber, r.ReviewedAtUtc))
            .ToListAsync(ct);

        var summary = new MissionSummary(
            assignment.Id, assignment.StudentProfileId, studentName, assignment.Title,
            assignment.Status, assignment.CurrentRound, assignment.SubmittedForReviewAtUtc,
            assignment.CreatedAtUtc, assignment.XpReward, assignment.Rarity);

        return new MissionDetailForTeacher(summary, questions, latestAnswers, reviews);
    }

    // ── Teacher: review mission ───────────────────────────────────────────────

    public async Task ReviewMissionAsync(
        AuthenticatedUserProfile profile,
        Guid missionId,
        MissionReviewDecision decision,
        string? feedback,
        CancellationToken ct = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);

        var assignment = await _db.Assignments
            .FirstOrDefaultAsync(a => a.Id == missionId
                                   && a.SchoolId == schoolId
                                   && a.TeacherProfileId == teacherProfileId
                                   && a.IsMission, ct)
            ?? throw new KeyNotFoundException("Missao nao encontrada.");

        if (assignment.Status != AssignmentStatus.AwaitingReview)
            throw new InvalidOperationException("Missao nao esta aguardando revisao.");

        var now = DateTime.UtcNow;
        var roundNumber = assignment.CurrentRound;

        var review = new MissionReview(missionId, teacherProfileId, decision, feedback, roundNumber);
        _db.MissionReviews.Add(review);

        if (decision == MissionReviewDecision.Approved)
        {
            assignment.Approve(now);

            if (!assignment.XpGranted && assignment.XpReward > 0)
            {
                var student = await _db.StudentProfiles.FirstAsync(
                    s => s.Id == assignment.StudentProfileId && s.SchoolId == schoolId, ct);

                student.CurrentXp      += assignment.XpReward;
                student.CurrentLevelXp += assignment.XpReward;
                assignment.MarkXpGranted();

                _db.XpEvents.Add(new XpEvent(
                    schoolId, assignment.StudentProfileId, XpEventType.AssignmentCompleted,
                    assignment.XpReward, $"Missao aprovada: {assignment.Title}", now,
                    teacherProfileId, assignment.Id));

                await TryAutoLevelUpAsync(schoolId, assignment.StudentProfileId, student, now, ct);
            }

            if (assignment.SkillRewardId.HasValue)
            {
                var alreadyMastered = await _db.StudentSkillMasteries.AnyAsync(
                    m => m.SchoolId == schoolId
                      && m.StudentProfileId == assignment.StudentProfileId
                      && m.SkillId == assignment.SkillRewardId.Value, ct);

                if (!alreadyMastered)
                {
                    _db.StudentSkillMasteries.Add(new StudentSkillMastery(
                        schoolId, teacherProfileId, assignment.StudentProfileId,
                        assignment.SkillRewardId.Value, now));
                }
            }
        }
        else
        {
            assignment.RequestAdjustments(now);
        }

        await _db.SaveChangesAsync(ct);
    }

    // ── Teacher: download student answer file ────────────────────────────────

    public async Task<(string PhysicalPath, string ContentType, string FileName)?> GetAnswerFileAsync(
        AuthenticatedUserProfile profile,
        Guid missionId,
        Guid questionId,
        CancellationToken ct = default)
    {
        var (schoolId, teacherProfileId) = LearningAuthorization.RequireTeacher(profile);

        var assignment = await _db.Assignments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == missionId && a.SchoolId == schoolId
                                   && a.TeacherProfileId == teacherProfileId && a.IsMission, ct);
        if (assignment is null) return null;

        var answer = await _db.MissionAnswers.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AssignmentId == missionId && a.QuestionId == questionId
                                   && a.RoundNumber == assignment.CurrentRound, ct);
        if (answer?.StoredFileName is null) return null;

        var path = Path.Combine(_env.ContentRootPath, UploadRoot,
            missionId.ToString(), questionId.ToString(), answer.StoredFileName);
        if (!File.Exists(path)) return null;

        return (path, answer.ContentType ?? "application/octet-stream", answer.OriginalFileName ?? answer.StoredFileName);
    }

    // ── Student: list missions ────────────────────────────────────────────────

    public async Task<IReadOnlyList<MissionDetailForStudent>> ListForCurrentStudentAsync(
        AuthenticatedUserProfile profile,
        CancellationToken ct = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);

        var missions = await _db.Assignments
            .AsNoTracking()
            .Where(a => a.SchoolId == schoolId && a.StudentProfileId == studentProfileId
                     && a.IsMission && a.Status != AssignmentStatus.Skipped)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(ct);

        var result = new List<MissionDetailForStudent>(missions.Count);
        foreach (var m in missions)
        {
            result.Add(new MissionDetailForStudent(
                m.Id, m.Title, m.Description, m.DueAtUtc, m.Status, m.CurrentRound,
                m.XpReward, m.Rarity, m.CreatedAtUtc,
                Array.Empty<MissionQuestionDto>(),
                Array.Empty<MissionAnswerDto>(),
                null));
        }
        return result;
    }

    // ── Student: get mission detail with questions and current answers ────────

    public async Task<MissionDetailForStudent?> GetMissionDetailForStudentAsync(
        AuthenticatedUserProfile profile,
        Guid missionId,
        CancellationToken ct = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);

        var assignment = await _db.Assignments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == missionId && a.SchoolId == schoolId
                                   && a.StudentProfileId == studentProfileId && a.IsMission, ct);
        if (assignment is null) return null;

        var questions = await _db.MissionQuestions.AsNoTracking()
            .Where(q => q.AssignmentId == missionId)
            .OrderBy(q => q.Order)
            .Select(q => new MissionQuestionDto(
                q.Id,
                q.QuestionText,
                q.QuestionType,
                q.Order,
                q.IsRequired,
                ReadOptions(q.OptionsJson)))
            .ToListAsync(ct);

        var currentAnswers = await _db.MissionAnswers.AsNoTracking()
            .Where(a => a.AssignmentId == missionId && a.RoundNumber == assignment.CurrentRound)
            .Select(a => new MissionAnswerDto(
                a.QuestionId,
                a.AnswerText,
                a.OriginalFileName,
                a.ContentType,
                a.SizeBytes,
                a.StoredFileName != null
                    ? $"/Student/Missions/{missionId}/Answers/{a.QuestionId}/File"
                    : null))
            .ToListAsync(ct);

        var lastReview = await _db.MissionReviews.AsNoTracking()
            .Where(r => r.AssignmentId == missionId)
            .OrderByDescending(r => r.ReviewedAtUtc)
            .Select(r => new MissionReviewDto(r.Id, r.Decision, r.Feedback, r.RoundNumber, r.ReviewedAtUtc))
            .FirstOrDefaultAsync(ct);

        return new MissionDetailForStudent(
            assignment.Id, assignment.Title, assignment.Description, assignment.DueAtUtc,
            assignment.Status, assignment.CurrentRound, assignment.XpReward, assignment.Rarity,
            assignment.CreatedAtUtc, questions, currentAnswers, lastReview);
    }

    // ── Student: save or update answers (draft or pre-submit) ────────────────

    public async Task SaveAnswersAsync(
        AuthenticatedUserProfile profile,
        Guid missionId,
        IReadOnlyList<(Guid QuestionId, string? Text, IFormFile? File)> answers,
        bool submit,
        CancellationToken ct = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);

        var assignment = await _db.Assignments
            .FirstOrDefaultAsync(a => a.Id == missionId && a.SchoolId == schoolId
                                   && a.StudentProfileId == studentProfileId && a.IsMission, ct)
            ?? throw new KeyNotFoundException("Missao nao encontrada.");

        if (assignment.Status == AssignmentStatus.Completed
            || assignment.Status == AssignmentStatus.Skipped)
        {
            throw new InvalidOperationException("Esta missao nao pode mais ser editada.");
        }

        if (assignment.Status == AssignmentStatus.AwaitingReview && !submit)
        {
            throw new InvalidOperationException("A missao ja foi enviada para revisao.");
        }

        var now = DateTime.UtcNow;
        var round = assignment.CurrentRound;

        // Validate all required questions are answered when submitting
        if (submit)
        {
            var requiredIds = await _db.MissionQuestions
                .Where(q => q.AssignmentId == missionId && q.IsRequired)
                .Select(q => q.Id)
                .ToListAsync(ct);

            var answeredIds = answers
                .Where(a => !string.IsNullOrWhiteSpace(a.Text) || a.File is not null)
                .Select(a => a.QuestionId)
                .ToHashSet();

            var existing = await _db.MissionAnswers
                .Where(a => a.AssignmentId == missionId && a.RoundNumber == round
                         && (!string.IsNullOrEmpty(a.AnswerText) || a.StoredFileName != null))
                .Select(a => a.QuestionId)
                .ToListAsync(ct);

            answeredIds.UnionWith(existing);

            var missing = requiredIds.Where(id => !answeredIds.Contains(id)).ToList();
            if (missing.Any())
                throw new InvalidOperationException("Responda todas as perguntas obrigatorias antes de enviar.");
        }

        foreach (var (questionId, text, file) in answers)
        {
            var existing = await _db.MissionAnswers
                .FirstOrDefaultAsync(a => a.AssignmentId == missionId
                                       && a.QuestionId == questionId
                                       && a.RoundNumber == round, ct);

            if (existing is null)
            {
                existing = new MissionAnswer(missionId, questionId, studentProfileId, round);
                _db.MissionAnswers.Add(existing);
            }

            var question = await _db.MissionQuestions
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == questionId && q.AssignmentId == missionId, ct);

            if (question is null)
                throw new InvalidOperationException("Pergunta da missao invalida.");

            if (!string.IsNullOrWhiteSpace(text))
            {
                var cleanText = text.Trim();
                if (question.QuestionType == MissionQuestionType.MultipleChoice)
                {
                    var options = ReadOptions(question.OptionsJson);
                    if (!options.Contains(cleanText, StringComparer.Ordinal))
                        throw new InvalidOperationException("Alternativa selecionada invalida.");
                }

                existing.AnswerText = cleanText;
            }

            if (file is not null)
            {
                var maxBytes = question?.QuestionType == MissionQuestionType.Audio
                    ? MaxAudioBytes
                    : MaxFileBytes;

                if (file.Length > maxBytes)
                    throw new InvalidOperationException($"Arquivo '{file.FileName}' excede o tamanho maximo permitido.");

                ValidateUpload(question?.QuestionType, file.FileName, file.ContentType);

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var storedName = $"{Guid.NewGuid():N}{ext}";
                var dir = Path.Combine(_env.ContentRootPath, UploadRoot,
                    missionId.ToString(), questionId.ToString());
                Directory.CreateDirectory(dir);

                await using var fs = File.Create(Path.Combine(dir, storedName));
                await file.CopyToAsync(fs, ct);

                // Delete previous file if exists
                if (existing.StoredFileName is not null)
                {
                    var oldPath = Path.Combine(dir, existing.StoredFileName);
                    if (File.Exists(oldPath)) File.Delete(oldPath);
                }

                existing.StoredFileName = storedName;
                existing.OriginalFileName = SanitizeFileName(file.FileName);
                existing.ContentType = file.ContentType;
                existing.SizeBytes = file.Length;
            }

            existing.UpdatedAtUtc = now;
        }

        if (assignment.Status == AssignmentStatus.Pending
            || assignment.Status == AssignmentStatus.InProgress)
        {
            assignment.Start(now);
        }

        if (submit)
            assignment.SubmitForReview(now);

        await _db.SaveChangesAsync(ct);
    }

    // ── Student: download own answer file ────────────────────────────────────

    public async Task<(string PhysicalPath, string ContentType, string FileName)?> GetStudentAnswerFileAsync(
        AuthenticatedUserProfile profile,
        Guid missionId,
        Guid questionId,
        CancellationToken ct = default)
    {
        var (schoolId, studentProfileId) = LearningAuthorization.RequireStudent(profile);

        var assignment = await _db.Assignments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == missionId && a.SchoolId == schoolId
                                   && a.StudentProfileId == studentProfileId && a.IsMission, ct);
        if (assignment is null) return null;

        var answer = await _db.MissionAnswers.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AssignmentId == missionId && a.QuestionId == questionId
                                   && a.RoundNumber == assignment.CurrentRound, ct);
        if (answer?.StoredFileName is null) return null;

        var path = Path.Combine(_env.ContentRootPath, UploadRoot,
            missionId.ToString(), questionId.ToString(), answer.StoredFileName);
        if (!File.Exists(path)) return null;

        return (path, answer.ContentType ?? "application/octet-stream", answer.OriginalFileName ?? answer.StoredFileName);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Dictionary<Guid, string>> StudentNameMapAsync(
        IEnumerable<Guid> studentProfileIds,
        CancellationToken ct)
    {
        var ids = studentProfileIds.Distinct().ToList();
        var profiles = await _db.StudentProfiles.AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .Select(s => new { s.Id, s.SchoolUserId })
            .ToListAsync(ct);

        var userIds = profiles.Select(p => p.SchoolUserId).Distinct().ToList();
        var users = await _db.SchoolUsers.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName })
            .ToListAsync(ct);

        var userMap = users.ToDictionary(u => u.Id, u => u.DisplayName);
        return profiles.ToDictionary(
            p => p.Id,
            p => userMap.GetValueOrDefault(p.SchoolUserId, "Aluno"));
    }

    private static IReadOnlyList<string> NormalizeQuestionOptions(CreateMissionQuestionRequest question)
    {
        if (question.QuestionType != MissionQuestionType.MultipleChoice)
            return Array.Empty<string>();

        var options = (question.Options ?? Array.Empty<string>())
            .Select(option => option.Trim())
            .Where(option => !string.IsNullOrWhiteSpace(option))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (options.Length < 2 || options.Length > 4)
            throw new ArgumentException("Perguntas de alternativa precisam ter entre 2 e 4 opcoes.");

        return options;
    }

    private static IReadOnlyList<string> ReadOptions(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
            return Array.Empty<string>();

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<string>>(optionsJson) ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private static void ValidateUpload(MissionQuestionType? type, string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (type == MissionQuestionType.Audio)
        {
            var allowedAudioExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".mp3", ".wav", ".ogg", ".webm", ".m4a"
            };

            if (!allowedAudioExtensions.Contains(ext)
                || !contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Esta pergunta aceita apenas audio MP3, WAV, OGG, WebM ou M4A.");
            }

            return;
        }

        var allowedFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".pdf", ".txt", ".doc", ".docx", ".rtf", ".musicxml", ".xml"
        };

        var allowedMime = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("text/plain", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/rtf", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/msword", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/xml", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("text/xml", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase);

        if (!allowedFileExtensions.Contains(ext) || !allowedMime)
        {
            throw new InvalidOperationException("Arquivo nao permitido. Use imagem, PDF, documento, cifra, partitura ou MusicXML.");
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var clean = Path.GetFileName(fileName);
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            clean = clean.Replace(c, '_');
        }

        return string.IsNullOrWhiteSpace(clean) ? "arquivo" : clean;
    }

    private async Task TryAutoLevelUpAsync(
        Guid schoolId, Guid studentProfileId, StudentProfile student,
        DateTime now, CancellationToken ct)
    {
        while (LearningLevelCalculator.IsEligibleForPromotion(student.CurrentLevelXp, student.CurrentLevel))
        {
            var hasRequiredSkill = await _db.Skills.AnyAsync(
                s => s.SchoolId == schoolId
                  && s.RequiredLevel == student.CurrentLevel
                  && s.IsActive
                  && s.SkillType == SkillType.PromotionRequired, ct);
            if (hasRequiredSkill) break;

            var from = student.CurrentLevel;
            student.CurrentLevel   += 1;
            student.CurrentLevelXp  = 0;
            _db.LevelUpEvents.Add(new LevelUpEvent(schoolId, studentProfileId, from, student.CurrentLevel, now));
        }
    }
}
