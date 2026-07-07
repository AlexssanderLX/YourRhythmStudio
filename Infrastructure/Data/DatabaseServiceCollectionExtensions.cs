using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Application.Learning;
using YourRhythmStudio.Application.Users;

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
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));
        services.AddScoped<IUserDirectoryService, UserDirectoryService>();
        services.AddScoped<IUserProfileResolver, UserProfileResolver>();
        services.AddScoped<TeacherStudentService>();
        services.AddScoped<LessonService>();
        services.AddScoped<RepertoireService>();
        services.AddScoped<AssignmentService>();
        services.AddScoped<FeedbackService>();
        services.AddScoped<ProgressService>();

        return services;
    }
}
