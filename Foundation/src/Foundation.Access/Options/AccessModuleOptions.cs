namespace Foundation.Access.Options;

public sealed class AccessModuleOptions
{
    public int CodeLength { get; init; } = 6;

    public TimeSpan ChallengeTimeToLive { get; init; } = TimeSpan.FromMinutes(10);

    public TimeSpan SessionTimeToLive { get; init; } = TimeSpan.FromHours(12);

    public int MaxAttempts { get; init; } = 5;

    public string CodeSubjectPrefix { get; init; } = "Seu codigo de acesso";

    public bool AllowSelfServiceRegistration { get; init; } = true;

    public bool RequireAdministratorApprovalForRegistration { get; init; } = true;

    public bool RequireActiveSubscriptionForTenantAccess { get; init; }

    public int DefaultTrialDays { get; init; } = 14;

    public int PasswordMinLength { get; init; } = 8;

    public bool RequireUppercaseInPassword { get; init; } = true;

    public bool RequireLowercaseInPassword { get; init; } = true;

    public bool RequireDigitInPassword { get; init; } = true;

    public bool RequireSpecialCharacterInPassword { get; init; }

    public int PasswordHashIterations { get; init; } = 100_000;

    public int PasswordSaltSizeBytes { get; init; } = 16;

    public int PasswordHashSizeBytes { get; init; } = 32;

    public IReadOnlyCollection<string> AdminReviewRecipients { get; init; } = Array.Empty<string>();
}
