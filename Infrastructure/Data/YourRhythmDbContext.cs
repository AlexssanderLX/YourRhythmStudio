using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Domain.Learning;
using YourRhythmStudio.Domain.Root;
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

    public DbSet<TeacherStudent> TeacherStudents => Set<TeacherStudent>();

    public DbSet<Lesson> Lessons => Set<Lesson>();

    public DbSet<RepertoireItem> RepertoireItems => Set<RepertoireItem>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<FeedbackEntry> FeedbackEntries => Set<FeedbackEntry>();

    public DbSet<XpEvent> XpEvents => Set<XpEvent>();

    public DbSet<Skill> Skills => Set<Skill>();

    public DbSet<StudentSkillMastery> StudentSkillMasteries => Set<StudentSkillMastery>();

    public DbSet<LevelConfig> LevelConfigs => Set<LevelConfig>();

    public DbSet<LevelUpEvent> LevelUpEvents => Set<LevelUpEvent>();

    public DbSet<PersistedAccount> PersistedAccounts => Set<PersistedAccount>();

    public DbSet<AccessRequest> AccessRequests => Set<AccessRequest>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    public DbSet<AdminSetting> AdminSettings => Set<AdminSetting>();

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
            entity.Property(school => school.PlanCode).HasMaxLength(40).IsRequired().HasDefaultValue("professor");
            entity.HasIndex(school => school.Slug).IsUnique();
        });

        modelBuilder.Entity<SchoolUser>(entity =>
        {
            entity.ToTable("school_users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.Property(user => user.Role).HasMaxLength(40).IsRequired();
            entity.Property(user => user.Phone).HasMaxLength(40);
            entity.Property(user => user.City).HasMaxLength(120);
            entity.Property(user => user.ProfilePhotoPath).HasMaxLength(260);
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

        modelBuilder.Entity<TeacherStudent>(entity =>
        {
            entity.ToTable("teacher_students");
            entity.HasKey(link => link.Id);
            entity.HasIndex(link => link.SchoolId);
            entity.HasIndex(link => link.TeacherProfileId);
            entity.HasIndex(link => link.StudentProfileId);
            entity.HasIndex(link => new { link.SchoolId, link.TeacherProfileId, link.StudentProfileId }).IsUnique();
            entity.HasOne<School>()
                .WithMany()
                .HasForeignKey(link => link.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<TeacherProfile>()
                .WithMany()
                .HasForeignKey(link => link.TeacherProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<StudentProfile>()
                .WithMany()
                .HasForeignKey(link => link.StudentProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.ToTable("lessons");
            entity.HasKey(lesson => lesson.Id);
            entity.Property(lesson => lesson.Title).HasMaxLength(180).IsRequired();
            entity.Property(lesson => lesson.Notes).HasMaxLength(2000);
            entity.Property(lesson => lesson.Status).HasConversion<int>();
            entity.HasIndex(lesson => lesson.SchoolId);
            entity.HasIndex(lesson => lesson.TeacherProfileId);
            entity.HasIndex(lesson => lesson.StudentProfileId);
            entity.HasIndex(lesson => new { lesson.SchoolId, lesson.StudentProfileId, lesson.LessonDateUtc });
            entity.HasOne<School>()
                .WithMany()
                .HasForeignKey(lesson => lesson.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<TeacherProfile>()
                .WithMany()
                .HasForeignKey(lesson => lesson.TeacherProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<StudentProfile>()
                .WithMany()
                .HasForeignKey(lesson => lesson.StudentProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RepertoireItem>(entity =>
        {
            entity.ToTable("repertoire_items");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Title).HasMaxLength(180).IsRequired();
            entity.Property(item => item.Notes).HasMaxLength(2000);
            entity.Property(item => item.ReferenceUrl).HasMaxLength(500);
            entity.Property(item => item.AudioStoredFileName).HasMaxLength(180);
            entity.Property(item => item.AudioOriginalFileName).HasMaxLength(260);
            entity.Property(item => item.AudioContentType).HasMaxLength(120);
            entity.Property(item => item.Status).HasConversion<int>();
            entity.HasIndex(item => item.SchoolId);
            entity.HasIndex(item => item.TeacherProfileId);
            entity.HasIndex(item => item.StudentProfileId);
            entity.HasIndex(item => new { item.SchoolId, item.StudentProfileId, item.Status });
            entity.HasOne<School>()
                .WithMany()
                .HasForeignKey(item => item.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<TeacherProfile>()
                .WithMany()
                .HasForeignKey(item => item.TeacherProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<StudentProfile>()
                .WithMany()
                .HasForeignKey(item => item.StudentProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.ToTable("assignments");
            entity.HasKey(assignment => assignment.Id);
            entity.Property(assignment => assignment.Title).HasMaxLength(180).IsRequired();
            entity.Property(assignment => assignment.Description).HasMaxLength(2000).IsRequired();
            entity.Property(assignment => assignment.Status).HasConversion<int>();
            entity.Property(assignment => assignment.Rarity).HasConversion<int>();
            entity.HasIndex(assignment => assignment.SchoolId);
            entity.HasIndex(assignment => assignment.TeacherProfileId);
            entity.HasIndex(assignment => assignment.StudentProfileId);
            entity.HasIndex(assignment => new { assignment.SchoolId, assignment.StudentProfileId, assignment.Status });
            entity.HasIndex(assignment => assignment.LessonId);
            entity.HasIndex(assignment => assignment.RepertoireItemId);
            entity.HasOne<School>()
                .WithMany()
                .HasForeignKey(assignment => assignment.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<TeacherProfile>()
                .WithMany()
                .HasForeignKey(assignment => assignment.TeacherProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<StudentProfile>()
                .WithMany()
                .HasForeignKey(assignment => assignment.StudentProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Lesson>()
                .WithMany()
                .HasForeignKey(assignment => assignment.LessonId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<RepertoireItem>()
                .WithMany()
                .HasForeignKey(assignment => assignment.RepertoireItemId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Skill>()
                .WithMany()
                .HasForeignKey(assignment => assignment.SkillRewardId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<FeedbackEntry>(entity =>
        {
            entity.ToTable("feedback_entries");
            entity.HasKey(feedback => feedback.Id);
            entity.Property(feedback => feedback.Message).HasMaxLength(2000).IsRequired();
            entity.HasIndex(feedback => feedback.SchoolId);
            entity.HasIndex(feedback => feedback.TeacherProfileId);
            entity.HasIndex(feedback => feedback.StudentProfileId);
            entity.HasIndex(feedback => new { feedback.SchoolId, feedback.StudentProfileId, feedback.VisibleToStudent });
            entity.HasIndex(feedback => feedback.LessonId);
            entity.HasIndex(feedback => feedback.AssignmentId);
            entity.HasIndex(feedback => feedback.RepertoireItemId);
            entity.HasOne<School>()
                .WithMany()
                .HasForeignKey(feedback => feedback.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<TeacherProfile>()
                .WithMany()
                .HasForeignKey(feedback => feedback.TeacherProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<StudentProfile>()
                .WithMany()
                .HasForeignKey(feedback => feedback.StudentProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Lesson>()
                .WithMany()
                .HasForeignKey(feedback => feedback.LessonId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Assignment>()
                .WithMany()
                .HasForeignKey(feedback => feedback.AssignmentId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<RepertoireItem>()
                .WithMany()
                .HasForeignKey(feedback => feedback.RepertoireItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.ToTable("skills");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).HasMaxLength(200).IsRequired();
            entity.Property(s => s.Description).HasMaxLength(1000);
            entity.Property(s => s.SkillType).HasConversion<int>();
            entity.Property(s => s.IconName).HasMaxLength(80);
            entity.Property(s => s.AchievementText).HasMaxLength(500);
            entity.Property(s => s.ConquestCriteria).HasMaxLength(500);
            entity.Property(s => s.SortOrder).HasDefaultValue(0);
            entity.Property(s => s.IsActive).HasDefaultValue(true);
            entity.HasIndex(s => new { s.SchoolId, s.TeacherProfileId, s.RequiredLevel });
            entity.HasOne<School>()
                .WithMany()
                .HasForeignKey(s => s.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<TeacherProfile>()
                .WithMany()
                .HasForeignKey(s => s.TeacherProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudentSkillMastery>(entity =>
        {
            entity.ToTable("student_skill_masteries");
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => new { m.SchoolId, m.StudentProfileId, m.SkillId }).IsUnique();
            entity.HasOne<School>()
                .WithMany()
                .HasForeignKey(m => m.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Skill>()
                .WithMany()
                .HasForeignKey(m => m.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<StudentProfile>()
                .WithMany()
                .HasForeignKey(m => m.StudentProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PersistedAccount>(entity =>
        {
            entity.ToTable("persisted_accounts");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(a => a.Email).HasMaxLength(256).IsRequired();
            entity.Property(a => a.Status).HasMaxLength(40).IsRequired();
            entity.Property(a => a.PlatformRole).HasMaxLength(40).IsRequired();
            entity.Property(a => a.PwdAlgorithm).HasMaxLength(40);
            entity.Property(a => a.PwdSaltBase64).HasMaxLength(256);
            entity.Property(a => a.PwdHashBase64).HasMaxLength(512);
            entity.Property(a => a.SecurityStamp).HasMaxLength(64);
            entity.HasIndex(a => a.Email).IsUnique();
        });

        modelBuilder.Entity<XpEvent>(entity =>
        {
            entity.ToTable("xp_events");
            entity.HasKey(xpEvent => xpEvent.Id);
            entity.Property(xpEvent => xpEvent.Description).HasMaxLength(300).IsRequired();
            entity.Property(xpEvent => xpEvent.Type).HasConversion<int>();
            entity.HasIndex(xpEvent => xpEvent.SchoolId);
            entity.HasIndex(xpEvent => xpEvent.TeacherProfileId);
            entity.HasIndex(xpEvent => xpEvent.StudentProfileId);
            entity.HasIndex(xpEvent => new { xpEvent.SchoolId, xpEvent.StudentProfileId, xpEvent.CreatedAtUtc });
            entity.HasIndex(xpEvent => xpEvent.SourceId);
            entity.HasOne<School>()
                .WithMany()
                .HasForeignKey(xpEvent => xpEvent.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<TeacherProfile>()
                .WithMany()
                .HasForeignKey(xpEvent => xpEvent.TeacherProfileId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<StudentProfile>()
                .WithMany()
                .HasForeignKey(xpEvent => xpEvent.StudentProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LevelConfig>(entity =>
        {
            entity.ToTable("level_configs");
            entity.HasKey(lc => lc.Id);
            entity.Property(lc => lc.Subtitle).HasMaxLength(200);
            entity.Property(lc => lc.Description).HasMaxLength(2000);
            entity.Property(lc => lc.TeacherExpectations).HasMaxLength(2000);
            entity.Property(lc => lc.Objectives).HasMaxLength(2000);
            entity.Property(lc => lc.ConquestMessage).HasMaxLength(500);
            entity.Property(lc => lc.OrientationMessage).HasMaxLength(1000);
            entity.HasIndex(lc => new { lc.SchoolId, lc.TeacherProfileId, lc.Level }).IsUnique();
            entity.HasOne<School>()
                .WithMany()
                .HasForeignKey(lc => lc.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<TeacherProfile>()
                .WithMany()
                .HasForeignKey(lc => lc.TeacherProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LevelUpEvent>(entity =>
        {
            entity.ToTable("level_up_events");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SchoolId, e.StudentProfileId, e.SeenAtUtc });
            entity.HasOne<School>()
                .WithMany()
                .HasForeignKey(e => e.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<StudentProfile>()
                .WithMany()
                .HasForeignKey(e => e.StudentProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<School>(e =>
        {
            e.Property(s => s.StorageUsedBytes).HasDefaultValue(0L);
            e.Property(s => s.StorageQuotaBytes).HasDefaultValue(0L);
        });

        modelBuilder.Entity<AccessRequest>(entity =>
        {
            entity.ToTable("access_requests");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.PlanCode).HasMaxLength(40).IsRequired();
            entity.Property(r => r.ResponsibleName).HasMaxLength(160).IsRequired();
            entity.Property(r => r.Email).HasMaxLength(256).IsRequired();
            entity.Property(r => r.SchoolName).HasMaxLength(160).IsRequired();
            entity.Property(r => r.Phone).HasMaxLength(40);
            entity.Property(r => r.Status).HasConversion<int>();
            entity.Property(r => r.ReviewNote).HasMaxLength(500);
            entity.Property(r => r.SetPasswordToken).HasMaxLength(128);
            entity.HasIndex(r => r.Email);
            entity.HasIndex(r => r.Status);
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.ToTable("plans");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Code).HasMaxLength(40).IsRequired();
            entity.Property(p => p.Name).HasMaxLength(120).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(500);
            entity.Property(p => p.MonthlyPriceBrl).HasColumnType("decimal(10,2)");
            entity.HasIndex(p => p.Code).IsUnique();
        });

        modelBuilder.Entity<AdminAuditLog>(entity =>
        {
            entity.ToTable("admin_audit_logs");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.ActorEmail).HasMaxLength(256).IsRequired();
            entity.Property(l => l.Action).HasMaxLength(80).IsRequired();
            entity.Property(l => l.TargetType).HasMaxLength(80);
            entity.Property(l => l.TargetId).HasMaxLength(80);
            entity.Property(l => l.Notes).HasMaxLength(500);
            entity.HasIndex(l => l.ActorAccountId);
            entity.HasIndex(l => new { l.TargetType, l.TargetId });
        });

        modelBuilder.Entity<AdminSetting>(entity =>
        {
            entity.ToTable("admin_settings");
            entity.HasKey(s => s.Key);
            entity.Property(s => s.Key).HasMaxLength(80).IsRequired();
            entity.Property(s => s.Value).HasMaxLength(500);
        });
    }
}
