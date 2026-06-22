using System.ComponentModel.DataAnnotations;

namespace YourRhythmStudio.ViewModels.Auth;

public class LoginViewModel
{
    [Required(ErrorMessage = "Informe seu e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail invalido.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe sua senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Escola")]
    public string? TenantKey { get; set; }

    [Display(Name = "Manter conectado")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
