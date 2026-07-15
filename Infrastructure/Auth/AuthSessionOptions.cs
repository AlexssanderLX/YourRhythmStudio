namespace YourRhythmStudio.Infrastructure.Auth;

public sealed class AuthSessionOptions
{
    public const string SectionName = "Authentication:Session";

    public int IdleTimeoutMinutes { get; set; } = 3;

    public int AbsoluteTimeoutMinutes { get; set; } = 30;

    public int ValidationIntervalSeconds { get; set; } = 60;

    public TimeSpan IdleTimeout => TimeSpan.FromMinutes(Math.Max(1, IdleTimeoutMinutes));

    public TimeSpan AbsoluteTimeout => TimeSpan.FromMinutes(Math.Max(IdleTimeoutMinutes, AbsoluteTimeoutMinutes));

    public TimeSpan ValidationInterval => TimeSpan.FromSeconds(Math.Max(15, ValidationIntervalSeconds));
}
