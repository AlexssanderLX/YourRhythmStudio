using System.ComponentModel.DataAnnotations;
using YourRhythmStudio.Application.Learning;

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
    public string Level { get; set; } = string.Empty;

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

public sealed class AddRepertoireViewModel
{
    [Required]
    public Guid StudentProfileId { get; set; }

    [Required(ErrorMessage = "Titulo da musica e obrigatorio.")]
    [StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [StringLength(180)]
    public string? ComposerOrArtist { get; set; }

    [StringLength(120)]
    public string? Instrument { get; set; }

    [StringLength(80)]
    public string? Level { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    [StringLength(500)]
    [Url(ErrorMessage = "Informe uma URL valida.")]
    public string? ReferenceUrl { get; set; }
}

public sealed class CreateAssignmentViewModel
{
    [Required]
    public Guid StudentProfileId { get; set; }

    [Required(ErrorMessage = "Titulo da missao e obrigatorio.")]
    [StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Descricao da missao e obrigatoria.")]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? DueAtUtc { get; set; }

    [Range(0, 600)]
    public int TargetMinutes { get; set; }

    [Range(0, 10000)]
    public int XpReward { get; set; } = 50;
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

public sealed class TeacherSkillsViewModel
{
    public required IReadOnlyCollection<SkillSummary> Skills { get; init; }
    public DefineSkillViewModel NewSkill { get; set; } = new();
}

public sealed class DefineSkillViewModel
{
    [Required(ErrorMessage = "Nome da habilidade Ã© obrigatÃ³rio.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(1, 5)]
    public int RequiredLevel { get; set; } = 1;
}

public sealed class TeacherStudentDetailWithSkillsViewModel
{
    public required TeacherStudentDetailViewModel Base { get; init; }
    public required IReadOnlyCollection<SkillWithMastery> Skills { get; init; }
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

