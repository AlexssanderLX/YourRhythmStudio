namespace YourRhythmStudio.Domain.Root;

public sealed class LandingTrack
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime UploadedAtUtc { get; init; } = DateTime.UtcNow;
}
