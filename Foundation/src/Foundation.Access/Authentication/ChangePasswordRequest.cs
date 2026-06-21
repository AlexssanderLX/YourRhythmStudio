namespace Foundation.Access.Authentication;

public sealed record ChangePasswordRequest(
    Guid AccountId,
    string CurrentPassword,
    string NewPassword,
    bool RevokeSessions = true);
