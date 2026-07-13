namespace YourRhythmStudio.Domain.Root;

public sealed class AdminAuditLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ActorAccountId { get; init; }
    public string ActorEmail { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string? TargetType { get; init; }
    public string? TargetId { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
