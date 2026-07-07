using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace YourRhythmStudio.Infrastructure.Data;

public sealed class YourRhythmDbContextFactory : IDesignTimeDbContextFactory<YourRhythmDbContext>
{
    public YourRhythmDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<YourRhythmDbContext>();
        optionsBuilder.UseMySql(
            "server=localhost;port=3306;database=yourrhythmstudio;user=yourrhythm_app;password=CHANGE_ME;",
            new MySqlServerVersion(new Version(8, 0, 36)));

        return new YourRhythmDbContext(optionsBuilder.Options);
    }
}

