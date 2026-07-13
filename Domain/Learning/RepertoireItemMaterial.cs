using YourRhythmStudio.Domain.Learning.Enums;

namespace YourRhythmStudio.Domain.Learning;

public sealed class RepertoireItemMaterial
{
    private RepertoireItemMaterial()
    {
    }

    public RepertoireItemMaterial(
        Guid repertoireItemId,
        Guid schoolId,
        RepertoireMaterialType materialType,
        string title,
        int order)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Material title is required.", nameof(title));

        Id = Guid.NewGuid();
        RepertoireItemId = repertoireItemId;
        SchoolId = schoolId;
        MaterialType = materialType;
        Title = title.Trim();
        Order = order;
        AddedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid RepertoireItemId { get; private set; }
    public Guid SchoolId { get; private set; }
    public RepertoireMaterialType MaterialType { get; private set; }
    public string Title { get; set; } = string.Empty;
    public string? StoredFileName { get; set; }
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public string? Url { get; set; }
    public int Order { get; set; }
    public DateTime AddedAtUtc { get; private set; }
}
