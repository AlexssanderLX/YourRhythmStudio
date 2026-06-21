using Microsoft.EntityFrameworkCore;

namespace YourRhythmStudio.Infrastructure.Data;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddYourRhythmDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        }

        services.AddDbContext<YourRhythmDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        return services;
    }
}
