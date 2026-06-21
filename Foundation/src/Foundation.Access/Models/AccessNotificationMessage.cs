namespace Foundation.Access.Models;

public sealed record AccessNotificationMessage(
    string Recipient,
    string Subject,
    string Body,
    string Code,
    DateTime ExpiresAtUtc);
