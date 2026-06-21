namespace Foundation.Access.Models;

public sealed record IssuedAccessChallenge(
    Guid ChallengeId,
    string MaskedRecipient,
    DateTime ExpiresAtUtc);
