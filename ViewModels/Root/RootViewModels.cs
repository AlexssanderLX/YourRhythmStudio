using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using YourRhythmStudio.Domain.Root;

namespace YourRhythmStudio.ViewModels.Root;

// ──── Dashboard ────────────────────────────────────────────────────────────────

public sealed class RootDashboardViewModel
{
    public int TotalAccounts { get; init; }
    public int PendingAccounts { get; init; }
    public int ActiveAccounts { get; init; }
    public int BlockedAccounts { get; init; }
    public int CancelledAccounts { get; init; }
    public int PendingRequests { get; init; }
    public List<PlanSummaryRow> PlanBreakdown { get; init; } = [];
}

public sealed class PlanSummaryRow
{
    public string PlanCode { get; init; } = string.Empty;
    public string PlanName { get; init; } = string.Empty;
    public int Count { get; init; }
}

// ──── Access Requests ──────────────────────────────────────────────────────────

public sealed class RequestListViewModel
{
    public List<RequestRow> Items { get; init; } = [];
    public string? StatusFilter { get; init; }
}

public sealed class RequestRow
{
    public Guid Id { get; init; }
    public string ResponsibleName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string SchoolName { get; init; } = string.Empty;
    public string PlanCode { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public AccessRequestStatus Status { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ReviewedAtUtc { get; init; }
}

public sealed class RequestDetailViewModel
{
    public Guid Id { get; init; }
    public string ResponsibleName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string SchoolName { get; init; } = string.Empty;
    public string PlanCode { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public AccessRequestStatus Status { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ReviewedAtUtc { get; init; }
    public string? ReviewNote { get; init; }
    public Guid? CreatedAccountId { get; init; }
}

public sealed class RejectRequestViewModel
{
    public Guid RequestId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}

// ──── Accounts ─────────────────────────────────────────────────────────────────

public sealed class AccountListViewModel
{
    public List<AccountRow> Items { get; init; } = [];
    public string? Search { get; init; }
    public string? StatusFilter { get; init; }
    public string? PlanFilter { get; init; }
}

public sealed class AccountRow
{
    public Guid AccountId { get; init; }
    public Guid? SchoolId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string SchoolName { get; init; } = string.Empty;
    public string PlanCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public long StorageUsedBytes { get; init; }
    public long StorageQuotaBytes { get; init; }
}

public sealed class AccountDetailViewModel
{
    public Guid AccountId { get; init; }
    public Guid? SchoolId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string SchoolName { get; init; } = string.Empty;
    public string PlanCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ActivatedAtUtc { get; init; }
    public long StorageUsedBytes { get; init; }
    public long StorageQuotaBytes { get; init; }
    public int StudentCount { get; init; }
    public int TeacherCount { get; init; }
    public List<AdminAuditLogRow> AuditLogs { get; init; } = [];
}

public sealed class AdminAuditLogRow
{
    public string ActorEmail { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public sealed class CreateAccountViewModel
{
    [Required(ErrorMessage = "Escolha um plano.")]
    [RegularExpression("professor|escola", ErrorMessage = "Plano invalido.")]
    [Display(Name = "Plano")]
    public string PlanCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o nome.")]
    [MaxLength(160)]
    [Display(Name = "Nome completo")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o nome da escola/studio.")]
    [MaxLength(160)]
    [Display(Name = "Nome da escola / studio")]
    public string SchoolName { get; set; } = string.Empty;

    [MaxLength(40)]
    [Display(Name = "Telefone (opcional)")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Defina uma senha temporaria.")]
    [MinLength(8)]
    [DataType(DataType.Password)]
    [Display(Name = "Senha temporaria")]
    public string Password { get; set; } = string.Empty;
}

public sealed class EditAccountViewModel
{
    public Guid AccountId { get; set; }

    [Required]
    [MaxLength(160)]
    [Display(Name = "Nome")]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(160)]
    [Display(Name = "Nome da escola / studio")]
    public string SchoolName { get; set; } = string.Empty;

    [MaxLength(40)]
    [Display(Name = "Telefone")]
    public string? Phone { get; set; }

    [Required]
    [Display(Name = "Plano")]
    public string PlanCode { get; set; } = string.Empty;
}

// ──── Plans ────────────────────────────────────────────────────────────────────

public sealed class PlansViewModel
{
    public List<PlanRow> Plans { get; init; } = [];
}

public sealed class PlanRow
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal MonthlyPriceBrl { get; init; }
    public int? MaxStudents { get; init; }
    public long StorageQuotaBytes { get; init; }
    public bool IsActive { get; init; }
    public int ActiveSchools { get; init; }
}

public sealed class UpsertPlanViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [MaxLength(40)]
    [Display(Name = "Codigo (slug)")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    [Display(Name = "Nome")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Descricao")]
    public string? Description { get; set; }

    [Range(0, 99999.99)]
    [Display(Name = "Preco mensal (R$)")]
    public decimal MonthlyPriceBrl { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Limite de alunos (0 = ilimitado)")]
    public int MaxStudentsInput { get; set; }

    [Range(1, long.MaxValue)]
    [Display(Name = "Cota de armazenamento (GB)")]
    public int StorageQuotaGb { get; set; } = 5;

    public bool IsActive { get; set; } = true;
}

// ──── Settings ─────────────────────────────────────────────────────────────────

public sealed class RootSettingsViewModel
{
    public string CurrentEmail { get; set; } = string.Empty;
    public string NotificationRecipient { get; set; } = string.Empty;
}

public sealed class UpdateRootCredentialsViewModel
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress]
    [Display(Name = "E-mail de login")]
    public string NewEmail { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "Minimo 8 caracteres.")]
    [Display(Name = "Nova senha (deixe em branco para manter)")]
    public string? NewPassword { get; set; }

    [Required(ErrorMessage = "Confirme sua senha atual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha atual")]
    public string CurrentPassword { get; set; } = string.Empty;
}

public sealed class UpdateNotificationRecipientViewModel
{
    [Required(ErrorMessage = "Informe o e-mail de destino.")]
    [EmailAddress]
    [Display(Name = "E-mail destinatario das notificacoes")]
    public string Recipient { get; set; } = string.Empty;
}

// ──── Soundtrack ───────────────────────────────────────────────────────────────

public sealed class SoundtrackViewModel
{
    public List<TrackRow> Tracks { get; init; } = [];
    public int MaxTracks { get; init; } = 10;
}

public sealed class TrackRow
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public DateTime UploadedAtUtc { get; init; }
}

public sealed class AddTrackViewModel
{
    [Required(ErrorMessage = "Informe o titulo da musica.")]
    [MaxLength(120)]
    [Display(Name = "Titulo")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecione um arquivo de audio.")]
    [Display(Name = "Arquivo de audio (mp3, ogg, wav, flac — max 20 MB)")]
    public IFormFile? File { get; set; }
}

// ──── Storage ──────────────────────────────────────────────────────────────────

public sealed class StorageOverviewViewModel
{
    public List<StorageRow> Items { get; init; } = [];
}

public sealed class StorageRow
{
    public Guid SchoolId { get; init; }
    public string SchoolName { get; init; } = string.Empty;
    public string OwnerEmail { get; init; } = string.Empty;
    public long UsedBytes { get; init; }
    public long QuotaBytes { get; init; }
    public int Percent => QuotaBytes > 0 ? (int)Math.Min(100, UsedBytes * 100 / QuotaBytes) : 0;
}

public sealed class EditStorageQuotaViewModel
{
    public Guid SchoolId { get; set; }

    [Required]
    [Range(1, 10000)]
    [Display(Name = "Nova cota (GB)")]
    public int QuotaGb { get; set; }
}
