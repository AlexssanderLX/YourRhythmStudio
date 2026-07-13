using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YourRhythmStudio.ViewModels.Auth;

public class RegisterViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Escolha um plano.")]
    [RegularExpression("professor|escola", ErrorMessage = "Plano invalido.")]
    public string PlanCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu nome.")]
    [MaxLength(160)]
    [Display(Name = "Nome completo do responsavel")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail invalido.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(160)]
    [Display(Name = "Nome da escola / studio")]
    public string? SchoolName { get; set; }

    [MaxLength(40)]
    [Display(Name = "Telefone / WhatsApp (opcional)")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Crie uma senha.")]
    [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme sua senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar senha")]
    [Compare(nameof(Password), ErrorMessage = "As senhas nao coincidem.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IReadOnlyCollection<RegisterPlanOptionViewModel> PlanOptions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PlanCode == "escola" && string.IsNullOrWhiteSpace(SchoolName))
        {
            yield return new ValidationResult(
                "Informe o nome da escola ou studio.",
                new[] { nameof(SchoolName) });
        }
    }
}

public sealed class RegisterPlanOptionViewModel
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal MonthlyPriceBrl { get; init; }
    public int? MaxStudents { get; init; }
    public bool IsAvailableForRequest { get; init; } = true;
    public bool IsSchoolPlan => Code.Equals("escola", StringComparison.OrdinalIgnoreCase);
}

public class SetPasswordViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Crie uma senha.")]
    [MinLength(8, ErrorMessage = "Minimo 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nova senha")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme sua senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar senha")]
    [Compare(nameof(Password), ErrorMessage = "As senhas nao coincidem.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
