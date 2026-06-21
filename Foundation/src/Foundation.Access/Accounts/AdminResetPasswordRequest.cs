namespace Foundation.Access.Accounts;

public sealed record AdminResetPasswordRequest(
    Guid RequestedByAccountId,
    Guid TargetAccountId,
    string NewPassword,
    bool RevokeSessions = true);
