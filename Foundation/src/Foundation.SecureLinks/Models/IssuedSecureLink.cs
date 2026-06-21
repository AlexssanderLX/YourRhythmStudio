namespace Foundation.SecureLinks.Models;

public sealed record IssuedSecureLink(
    Guid LinkId,
    string PublicCode,
    string AbsoluteUrl,
    DateTime? ExpiresAtUtc,
    int? MaxUsages);
