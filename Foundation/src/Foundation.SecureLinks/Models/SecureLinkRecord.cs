namespace Foundation.SecureLinks.Models;

public sealed class SecureLinkRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Label { get; init; } = string.Empty;

    public string ResourceKey { get; init; } = string.Empty;

    public string RelativePath { get; init; } = string.Empty;

    public string PublicCode { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? ExpiresAtUtc { get; init; }

    public int? MaxUsages { get; init; }

    public int UsageCount { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsExpired(DateTime utcNow) => ExpiresAtUtc.HasValue && utcNow >= ExpiresAtUtc.Value;

    public bool HasUsageAvailable => !MaxUsages.HasValue || UsageCount < MaxUsages.Value;
}
