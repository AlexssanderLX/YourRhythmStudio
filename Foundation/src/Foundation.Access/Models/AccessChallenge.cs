namespace Foundation.Access.Models;

public sealed class AccessChallenge
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string SubjectId { get; init; } = string.Empty;

    public string SubjectDisplayName { get; init; } = string.Empty;

    public string Recipient { get; init; } = string.Empty;

    public string Purpose { get; init; } = string.Empty;

    public string CodeHash { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime ExpiresAtUtc { get; init; }

    public int MaxAttempts { get; init; }

    public int AttemptCount { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;

    public bool HasAttemptsRemaining => AttemptCount < MaxAttempts;
}
