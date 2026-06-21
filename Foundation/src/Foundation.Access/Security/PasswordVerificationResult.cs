namespace Foundation.Access.Security;

public sealed record PasswordVerificationResult(
    bool IsSuccess,
    bool NeedsRehash);
