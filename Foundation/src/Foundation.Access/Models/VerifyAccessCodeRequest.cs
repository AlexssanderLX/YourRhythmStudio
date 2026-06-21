namespace Foundation.Access.Models;

public sealed record VerifyAccessCodeRequest(
    Guid ChallengeId,
    string Code);
