using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using YourRhythmStudio.Application.Learning;
using YourRhythmStudio.Domain.Learning.Enums;

namespace YourRhythmStudio.ViewModels.Learning;

// ── Teacher mission creation ──────────────────────────────────────────────────

public sealed class CreateMissionViewModel
{
    [Required]
    public Guid StudentProfileId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Titulo da missao e obrigatorio.")]
    [StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Descricao da missao e obrigatoria.")]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public DateTime? DueAtLocal { get; set; }

    [Range(0, 10000)]
    public int XpReward { get; set; } = 100;

    public AssignmentRarity Rarity { get; set; } = AssignmentRarity.Comum;

    public Guid? SkillRewardId { get; set; }

    // Serialized as JSON from the form builder JS
    public string QuestionsJson { get; set; } = "[]";
}

public sealed class MissionQuestionFormItem
{
    public string QuestionText { get; set; } = string.Empty;
    public MissionQuestionType QuestionType { get; set; } = MissionQuestionType.Text;
    public bool IsRequired { get; set; } = true;
}

// ── Teacher mission review ────────────────────────────────────────────────────

public sealed class ReviewMissionViewModel
{
    [Required]
    public Guid MissionId { get; set; }

    [Required]
    public MissionReviewDecision Decision { get; set; }

    [StringLength(3000)]
    public string? Feedback { get; set; }
}

// ── Teacher Devolutivas ───────────────────────────────────────────────────────

public sealed class DevolutivasViewModel
{
    public required IReadOnlyList<MissionSummary> Pending { get; init; }
}

// ── Teacher mission detail ────────────────────────────────────────────────────

public sealed class TeacherMissionDetailViewModel
{
    public required MissionDetailForTeacher Detail { get; init; }
    public ReviewMissionViewModel Review { get; set; } = new();
}

// ── Student mission list ──────────────────────────────────────────────────────

public sealed class StudentMissionsViewModel
{
    public required IReadOnlyList<MissionDetailForStudent> Missions { get; init; }
}

// ── Student mission detail ────────────────────────────────────────────────────

public sealed class StudentMissionDetailViewModel
{
    public required MissionDetailForStudent Detail { get; init; }
}

// ── Answer upload per question ────────────────────────────────────────────────

public sealed class MissionAnswerUploadViewModel
{
    public Guid MissionId { get; set; }
    // Key: questionId.ToString(); Value: text or file
    public Dictionary<string, string?> TextAnswers { get; set; } = new();
    public Dictionary<string, IFormFile?> FileAnswers { get; set; } = new();
}

// ── Repertoire material add ───────────────────────────────────────────────────

public sealed class AddRepertoireMaterialViewModel
{
    [Required]
    public Guid RepertoireItemId { get; set; }

    [Required]
    public Guid StudentProfileId { get; set; }

    [Required(ErrorMessage = "Titulo do material e obrigatorio.")]
    [StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public RepertoireMaterialType MaterialType { get; set; }

    public string? Url { get; set; }

    public IFormFile? File { get; set; }
}
