using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace YourRhythmStudio.ViewModels.Settings;

public sealed class AccountSettingsPageViewModel
{
    public string Role { get; init; } = "student";
    public StudentAccountSettingsViewModel? Student { get; init; }
    public TeacherAccountSettingsViewModel? Teacher { get; init; }
}

public sealed class StudentAccountSettingsViewModel
{
    public required StudentProfileFormViewModel Profile { get; init; }
}

public sealed class TeacherAccountSettingsViewModel
{
    public required TeacherProfilePhotoFormViewModel Profile { get; init; }
    public required TeacherEmailFormViewModel Email { get; init; }
    public required TeacherPasswordFormViewModel Password { get; init; }
}

public sealed class StudentProfileFormViewModel
{
    [Required(ErrorMessage = "Nome e obrigatorio.")]
    [StringLength(160)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(120)]
    public string? ExternalContact { get; set; }

    public string Instrument { get; set; } = string.Empty;

    public int CurrentLevel { get; set; }

    public string CurrentLevelBadge { get; set; } = string.Empty;

    public string? ProfilePhotoUrl { get; set; }

    public IFormFile? Photo { get; set; }

    public bool RemovePhoto { get; set; }
}

public sealed class TeacherProfilePhotoFormViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? ProfilePhotoUrl { get; set; }

    public IFormFile? Photo { get; set; }

    public bool RemovePhoto { get; set; }
}

public sealed class TeacherEmailFormViewModel
{
    public string CurrentEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Novo e-mail e obrigatorio.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail valido.")]
    [StringLength(256)]
    public string NewEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha atual e obrigatoria.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;
}

public sealed class TeacherPasswordFormViewModel
{
    [Required(ErrorMessage = "Senha atual e obrigatoria.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nova senha e obrigatoria.")]
    [DataType(DataType.Password)]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a nova senha.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "As senhas nao coincidem.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
