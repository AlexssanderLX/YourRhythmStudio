using Foundation.Access.Security;

namespace Foundation.Access.Registrations;

public sealed class TenantRegistrationRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string TenantDisplayName { get; set; } = string.Empty;

    public string TenantKey { get; set; } = string.Empty;

    public string OwnerDisplayName { get; set; } = string.Empty;

    public string OwnerEmail { get; set; } = string.Empty;

    public PasswordCredential PasswordCredential { get; set; } = new();

    public string? RequestedPlanCode { get; set; }

    public RegistrationRequestStatus Status { get; set; } = RegistrationRequestStatus.Pending;

    public string? ReviewNotes { get; set; }

    public Guid? ReviewedByAccountId { get; set; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? ReviewedAtUtc { get; set; }
}
