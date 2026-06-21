namespace Foundation.SecureLinks.Models;

public sealed record SecureLinkResolution(
    Guid LinkId,
    string PublicCode,
    string ResourceKey,
    string AbsoluteUrl,
    int UsageCount,
    bool IsExpired,
    bool IsActive);
