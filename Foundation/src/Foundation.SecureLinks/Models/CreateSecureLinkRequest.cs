namespace Foundation.SecureLinks.Models;

public sealed record CreateSecureLinkRequest(
    string Label,
    string ResourceKey,
    string RelativePath,
    DateTime? ExpiresAtUtc = null,
    int? MaxUsages = null);
