using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Application.Users;

public sealed class AccountSettingsDto(
    Guid SchoolUserId,
    string DisplayName,
    string Email,
    string? Phone,
    string? City)
{
    public Guid SchoolUserId { get; } = SchoolUserId;
    public string DisplayName { get; } = DisplayName;
    public string Email { get; } = Email;
    public string? Phone { get; } = Phone;
    public string? City { get; } = City;
}

public sealed class TeacherProfileSettingsDto(string InstrumentFocus, string Bio)
{
    public string InstrumentFocus { get; } = InstrumentFocus;
    public string Bio { get; } = Bio;
}

public sealed class SettingsService
{
    private readonly YourRhythmDbContext _db;

    public SettingsService(YourRhythmDbContext db) => _db = db;

    public async Task<AccountSettingsDto?> GetAccountAsync(
        Guid schoolUserId, CancellationToken ct = default)
    {
        var user = await _db.SchoolUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == schoolUserId, ct);
        if (user is null) return null;
        return new AccountSettingsDto(user.Id, user.DisplayName, user.Email, user.Phone, user.City);
    }

    public async Task SaveAccountAsync(
        Guid schoolUserId,
        string displayName,
        string? phone,
        string? city,
        CancellationToken ct = default)
    {
        var user = await _db.SchoolUsers.FirstOrDefaultAsync(u => u.Id == schoolUserId, ct);
        if (user is null) return;
        user.DisplayName = displayName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        user.City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        await _db.SaveChangesAsync(ct);
    }

    public async Task<TeacherProfileSettingsDto?> GetTeacherProfileAsync(
        Guid teacherProfileId, CancellationToken ct = default)
    {
        var profile = await _db.TeacherProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == teacherProfileId, ct);
        if (profile is null) return null;
        return new TeacherProfileSettingsDto(profile.InstrumentFocus, profile.Bio);
    }

    public async Task SaveTeacherProfileAsync(
        Guid teacherProfileId,
        string instrumentFocus,
        string bio,
        CancellationToken ct = default)
    {
        var profile = await _db.TeacherProfiles.FirstOrDefaultAsync(p => p.Id == teacherProfileId, ct);
        if (profile is null) return;
        profile.InstrumentFocus = instrumentFocus.Trim();
        profile.Bio = bio.Trim();
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(string Name, string PlanCode)?> GetSchoolAsync(
        Guid schoolId, CancellationToken ct = default)
    {
        var school = await _db.Schools
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == schoolId, ct);
        if (school is null) return null;
        return (school.Name, school.PlanCode);
    }

    public async Task SaveSchoolNameAsync(
        Guid schoolId,
        string name,
        CancellationToken ct = default)
    {
        var school = await _db.Schools.FirstOrDefaultAsync(s => s.Id == schoolId, ct);
        if (school is null) return;
        school.Name = name.Trim();
        await _db.SaveChangesAsync(ct);
    }
}
