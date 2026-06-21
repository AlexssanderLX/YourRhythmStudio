using Microsoft.EntityFrameworkCore;

namespace YourRhythmStudio.Infrastructure.Data;

public sealed class YourRhythmDbContext : DbContext
{
    public YourRhythmDbContext(DbContextOptions<YourRhythmDbContext> options)
        : base(options)
    {
    }
}
