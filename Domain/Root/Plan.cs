namespace YourRhythmStudio.Domain.Root;

public sealed class Plan
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MonthlyPriceBrl { get; set; }
    public int? MaxStudents { get; set; }
    public long StorageQuotaBytes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
