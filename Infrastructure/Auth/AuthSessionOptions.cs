namespace YourRhythmStudio.Infrastructure.Auth;

public sealed class AuthSessionOptions
{
    public const string SectionName = "Authentication:Session";

    public const int MinimumIdleTimeoutMinutes = 60;

    public const int DefaultIdleTimeoutMinutes = 480;

    public const int DefaultAbsoluteTimeoutMinutes = 720;

    public int IdleTimeoutMinutes { get; set; } = DefaultIdleTimeoutMinutes;

    public int AbsoluteTimeoutMinutes { get; set; } = DefaultAbsoluteTimeoutMinutes;

    public int ValidationIntervalSeconds { get; set; } = 60;

    public TimeSpan IdleTimeout => TimeSpan.FromMinutes(Math.Max(MinimumIdleTimeoutMinutes, IdleTimeoutMinutes));

    public TimeSpan AbsoluteTimeout => TimeSpan.FromMinutes(Math.Max((int)IdleTimeout.TotalMinutes, AbsoluteTimeoutMinutes));

    public TimeSpan ValidationInterval => TimeSpan.FromSeconds(Math.Max(15, ValidationIntervalSeconds));
}
