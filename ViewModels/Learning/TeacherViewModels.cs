using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using YourRhythmStudio.Application.Learning;
using YourRhythmStudio.Domain.Learning.Enums;


namespace YourRhythmStudio.ViewModels.Learning;

public sealed class TeacherDashboardViewModel
{
    public required TeacherDashboardSummary Summary { get; init; }
}

public sealed class TeacherStudentsViewModel
{
    public required IReadOnlyCollection<TeacherStudentSummary> Students { get; init; }
}

public sealed class CreateStudentViewModel
{
    [Required(ErrorMessage = "Nome do aluno e obrigatorio.")]
    [StringLength(160)]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail do aluno e obrigatorio.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail valido.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(120)]
    public string Instrument { get; set; } = string.Empty;

    [StringLength(80)]
    public string Level { get; set; } = "1";

    [StringLength(1000)]
    public string Notes { get; set; } = string.Empty;
}

public sealed class TeacherStudentDetailViewModel
{
    public required StudentDetailSummary Detail { get; init; }
    public CreateLessonViewModel Lesson { get; set; } = new();
    public AddRepertoireViewModel Repertoire { get; set; } = new();
    public CreateAssignmentViewModel Assignment { get; set; } = new();
    public CreateFeedbackViewModel Feedback { get; set; } = new();
    public EditStudentViewModel EditStudent { get; set; } = new();
}

public sealed class EditStudentViewModel
{
    [Required]
    public Guid StudentProfileId { get; set; }

    [Required(ErrorMessage = "Nome do aluno e obrigatorio.")]
    [StringLength(160)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(120)]
    public string Instrument { get; set; } = string.Empty;

    [StringLength(80)]
    public string Level { get; set; } = "1";

    [StringLength(1000)]
    public string Notes { get; set; } = string.Empty;
}

public sealed class CreateLessonViewModel
{
    [Required]
    public Guid StudentProfileId { get; set; }

    [Required(ErrorMessage = "Titulo da aula e obrigatorio.")]
    [StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data da aula e obrigatoria.")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime LessonDateUtc { get; set; } = DateTime.UtcNow;

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public sealed class EditLessonViewModel
{
    [Required]
    public Guid StudentProfileId { get; set; }

    [Required]
    public Guid LessonId { get; set; }

    [Required(ErrorMessage = "Titulo da aula e obrigatorio.")]
    [StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data da aula e obrigatoria.")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime LessonDateUtc { get; set; } = DateTime.UtcNow;

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public sealed class TeacherLessonDetailViewModel
{
    public required LessonDetailSummary Detail { get; init; }
}

public sealed class AddRepertoireViewModel
{
    [Required]
    public Guid StudentProfileId { get; set; }

    [Required(ErrorMessage = "Nome do repertorio e obrigatorio.")]
    [StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Notes { get; set; }

    [StringLength(500)]
    [Url(ErrorMessage = "Informe uma URL valida.")]
    public string? ReferenceUrl { get; set; }

    public IFormFile? AudioFile { get; set; }
}

public sealed class EditRepertoireViewModel
{
    [Required]
    public Guid StudentProfileId { get; set; }

    [Required]
    public Guid RepertoireItemId { get; set; }

    [Required(ErrorMessage = "Nome do repertorio e obrigatorio.")]
    [StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Notes { get; set; }

    [StringLength(500)]
    [Url(ErrorMessage = "Informe uma URL valida.")]
    public string? ReferenceUrl { get; set; }

    public IFormFile? AudioFile { get; set; }
}

public sealed class CreateAssignmentViewModel
{
    [Required]
    public Guid StudentProfileId { get; set; }

    [Required(ErrorMessage = "Titulo da missao e obrigatorio.")]
    [StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? DueAtUtc { get; set; }

    [Range(0, 50000)]
    public int XpReward { get; set; } = 100;

    public AssignmentRarity Rarity { get; set; } = AssignmentRarity.Comum;

    public bool UseDefaultXp { get; set; } = true;

    public Guid? SkillRewardId { get; set; }
}

public sealed class EditAssignmentViewModel
{
    [Required]
    public Guid StudentProfileId { get; set; }

    [Required]
    public Guid AssignmentId { get; set; }

    [Required(ErrorMessage = "Titulo da missao e obrigatorio.")]
    [StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? DueAtUtc { get; set; }

    [Range(0, 50000)]
    public int XpReward { get; set; } = 100;

    public AssignmentRarity Rarity { get; set; } = AssignmentRarity.Comum;

    public Guid? SkillRewardId { get; set; }
}

public sealed class CreateFeedbackViewModel
{
    [Required]
    public Guid StudentProfileId { get; set; }

    [Required(ErrorMessage = "Feedback e obrigatorio.")]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    public bool VisibleToStudent { get; set; } = true;
}

public sealed class EditFeedbackViewModel
{
    [Required]
    public Guid StudentProfileId { get; set; }

    [Required]
    public Guid FeedbackId { get; set; }

    [Required(ErrorMessage = "Feedback e obrigatorio.")]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    public bool VisibleToStudent { get; set; } = true;
}

public sealed class TeacherSkillsViewModel
{
    public required IReadOnlyCollection<SkillSummary> Skills { get; init; }
    public DefineSkillViewModel NewSkill { get; set; } = new();
}

public sealed class DefineSkillViewModel
{
    [Required(ErrorMessage = "Nome da habilidade e obrigatorio.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(1, 5)]
    public int RequiredLevel { get; set; } = 1;

    public SkillType SkillType { get; set; } = SkillType.ProfessorSpecial;

    [StringLength(80)]
    public string? IconName { get; set; }

    [StringLength(500)]
    public string? AchievementText { get; set; }

    [StringLength(500)]
    public string? ConquestCriteria { get; set; }
}

public sealed class EditSkillViewModel
{
    [Required]
    public Guid SkillId { get; set; }

    [Required(ErrorMessage = "Nome da habilidade e obrigatorio.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(1, 5)]
    public int RequiredLevel { get; set; } = 1;

    public SkillType SkillType { get; set; } = SkillType.ProfessorSpecial;

    [StringLength(80)]
    public string? IconName { get; set; }

    [StringLength(500)]
    public string? AchievementText { get; set; }

    [StringLength(500)]
    public string? ConquestCriteria { get; set; }
}

public sealed record StudentAccessLinkViewModel(string Url, string QrCodeDataUri);

public sealed class TeacherLevelsViewModel
{
    public required IReadOnlyList<LevelConfigSummary> LevelConfigs { get; init; }
    public required IReadOnlyCollection<SkillSummary> Skills { get; init; }
}

public sealed class TeacherLevelDetailViewModel
{
    public required LevelConfigSummary Config { get; init; }
    public required IReadOnlyCollection<SkillSummary> Skills { get; init; }
    public SaveLevelConfigViewModel Form { get; set; } = new();
}

public sealed class SaveLevelConfigViewModel
{
    [Required]
    [Range(1, 5)]
    public int Level { get; set; }

    [StringLength(200)]
    public string? Subtitle { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(2000)]
    public string? TeacherExpectations { get; set; }

    [StringLength(2000)]
    public string? Objectives { get; set; }

    [StringLength(500)]
    public string? ConquestMessage { get; set; }

    [StringLength(1000)]
    public string? OrientationMessage { get; set; }
}

public sealed class TeacherStudentDetailWithSkillsViewModel
{
    public required TeacherStudentDetailViewModel Base { get; init; }
    public required IReadOnlyCollection<SkillWithMastery> Skills { get; init; }
    public ProgressSummary? Progress { get; init; }
    public string ActiveModule { get; init; } = "Summary";
    public StudentAccessLinkViewModel? AccessLink { get; init; }
}

public sealed class QuickLessonViewModel
{
    public required IReadOnlyCollection<TeacherStudentSummary> Students { get; init; }
    public QuickLessonFormViewModel Form { get; set; } = new();
}

public sealed class QuickLessonFormViewModel
{
    [Required]
    public Guid StudentProfileId { get; set; }

    [StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime LessonDateUtc { get; set; } = DateTime.Now;

    [StringLength(2000)]
    public string? Notes { get; set; }
}
