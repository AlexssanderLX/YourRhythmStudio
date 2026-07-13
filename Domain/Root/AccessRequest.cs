namespace YourRhythmStudio.Domain.Root;

public sealed class AccessRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string PlanCode { get; set; } = string.Empty;
    public string ResponsibleName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SchoolName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PasswordAlgorithm { get; set; }
    public int PasswordIterations { get; set; }
    public string? PasswordSaltBase64 { get; set; }
    public string? PasswordHashBase64 { get; set; }
    public DateTime? PasswordUpdatedAtUtc { get; set; }
    public AccessRequestStatus Status { get; set; } = AccessRequestStatus.Pending;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public Guid? ReviewedByAccountId { get; set; }
    public string? ReviewNote { get; set; }
    public string? SetPasswordToken { get; set; }
    public DateTime? SetPasswordTokenExpiresAtUtc { get; set; }
    public Guid? CreatedAccountId { get; set; }
}

public enum AccessRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
