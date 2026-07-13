namespace YourRhythmStudio.Domain.Root;

public sealed class AdminSetting
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
