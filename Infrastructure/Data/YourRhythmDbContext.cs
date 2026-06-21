using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Domain.Users;

namespace YourRhythmStudio.Infrastructure.Data;

public sealed class YourRhythmDbContext : DbContext
{
    public YourRhythmDbContext(DbContextOptions<YourRhythmDbContext> options)
        : base(options)
    {
    }

    public DbSet<School> Schools => Set<School>();

    public DbSet<SchoolUser> SchoolUsers => Set<SchoolUser>();

    public DbSet<TeacherProfile> TeacherProfiles => Set<TeacherProfile>();

    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<School>(entity =>
        {
            entity.ToTable("schools");
            entity.HasKey(school => school.Id);
            entity.Property(school => school.Name).HasMaxLength(160).IsRequired();
            entity.Property(school => school.Slug).HasMaxLength(180).IsRequired();
            entity.Property(school => school.PrimaryEmail).HasMaxLength(256).IsRequired();
            entity.HasIndex(school => school.Slug).IsUnique();
        });

        modelBuilder.Entity<SchoolUser>(entity =>
        {
            entity.ToTable("school_users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.Property(user => user.Role).HasMaxLength(40).IsRequired();
            entity.HasIndex(user => new { user.SchoolId, user.Email }).IsUnique();
            entity.HasOne(user => user.School)
                .WithMany(school => school.Users)
                .HasForeignKey(user => user.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeacherProfile>(entity =>
        {
            entity.ToTable("teacher_profiles");
            entity.HasKey(teacher => teacher.Id);
            entity.Property(teacher => teacher.InstrumentFocus).HasMaxLength(120);
            entity.Property(teacher => teacher.Bio).HasMaxLength(1000);
            entity.HasOne(teacher => teacher.School)
                .WithMany(school => school.Teachers)
                .HasForeignKey(teacher => teacher.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(teacher => teacher.SchoolUser)
                .WithOne()
                .HasForeignKey<TeacherProfile>(teacher => teacher.SchoolUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudentProfile>(entity =>
        {
            entity.ToTable("student_profiles");
            entity.HasKey(student => student.Id);
            entity.Property(student => student.Instrument).HasMaxLength(120);
            entity.Property(student => student.Level).HasMaxLength(80);
            entity.Property(student => student.Notes).HasMaxLength(1000);
            entity.HasOne(student => student.School)
                .WithMany(school => school.Students)
                .HasForeignKey(student => student.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(student => student.SchoolUser)
                .WithOne()
                .HasForeignKey<StudentProfile>(student => student.SchoolUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
