using System.Net.Mail;
using System.Text.RegularExpressions;
using Foundation.Access.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Domain;
using YourRhythmStudio.Infrastructure.Data;

namespace YourRhythmStudio.Application.Users;

public sealed record AccountSettingsDto(
    Guid SchoolUserId,
    string DisplayName,
    string Email,
    string? ExternalContact,
    string? City,
    string Role,
    string? ProfilePhotoUrl);

public sealed record StudentAccountSettingsDto(
    string DisplayName,
    string? ExternalContact,
    string Instrument,
    int CurrentLevel,
    string CurrentLevelBadge,
    string? ProfilePhotoUrl);

public sealed record TeacherAccountSettingsDto(
    string DisplayName,
    string Email,
    string? ProfilePhotoUrl);

public sealed class TeacherProfileSettingsDto(string InstrumentFocus, string Bio)
{
    public string InstrumentFocus { get; } = InstrumentFocus;
    public string Bio { get; } = Bio;
}

public sealed record UpdateStudentAccountRequest(
    string DisplayName,
    string? ExternalContact,
    IFormFile? Photo,
    bool RemovePhoto);

public sealed record UpdateTeacherPhotoRequest(
    IFormFile? Photo,
    bool RemovePhoto);

public sealed record ChangeTeacherEmailRequest(
    string NewEmail,
    string CurrentPassword);

public sealed record ChangeTeacherPasswordRequest(
    string CurrentPassword,
    string NewPassword);

public sealed record CredentialUpdateResult(
    string DisplayName,
    string Email);

public sealed class SettingsService
{
    private const long MaxPhotoBytes = 5 * 1024 * 1024;
    private static readonly Regex PhoneRegex = new(@"^[0-9\s()+.\-]{7,32}$", RegexOptions.Compiled);
    private static readonly HashSet<string> AllowedPhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly YourRhythmDbContext _db;
    private readonly IAccountStore _accountStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IWebHostEnvironment _environment;

    public SettingsService(
        YourRhythmDbContext db,
        IAccountStore accountStore,
        IPasswordHasher passwordHasher,
        IWebHostEnvironment environment)
    {
        _db = db;
        _accountStore = accountStore;
        _passwordHasher = passwordHasher;
        _environment = environment;
    }

    public async Task<AccountSettingsDto?> GetAccountAsync(
        Guid schoolUserId, CancellationToken ct = default)
    {
        var user = await _db.SchoolUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == schoolUserId, ct);
        if (user is null) return null;
        return new AccountSettingsDto(
            user.Id,
            user.DisplayName,
            user.Email,
            user.Phone,
            user.City,
            user.Role,
            ToPublicPhotoUrl(user.ProfilePhotoPath));
    }

    public async Task<StudentAccountSettingsDto> GetStudentAccountAsync(
        AuthenticatedUserProfile profile,
        CancellationToken ct = default)
    {
        RequireStudent(profile);

        var row = await (
            from student in _db.StudentProfiles.AsNoTracking()
            join user in _db.SchoolUsers.AsNoTracking() on student.SchoolUserId equals user.Id
            where student.SchoolId == profile.SchoolId
                && student.Id == profile.StudentProfileId
                && user.Id == profile.SchoolUserId
                && user.IsActive
            select new
            {
                user.DisplayName,
                ExternalContact = user.Phone,
                user.ProfilePhotoPath,
                student.Instrument,
                student.CurrentLevel
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException("Student account was not found.");

        return new StudentAccountSettingsDto(
            row.DisplayName,
            row.ExternalContact,
            row.Instrument,
            row.CurrentLevel,
            LevelBadge(row.CurrentLevel),
            ToPublicPhotoUrl(row.ProfilePhotoPath));
    }

    public async Task<TeacherAccountSettingsDto> GetTeacherAccountAsync(
        AuthenticatedUserProfile profile,
        CancellationToken ct = default)
    {
        RequireTeacher(profile);

        var user = await _db.SchoolUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == profile.SchoolUserId
                    && item.SchoolId == profile.SchoolId
                    && item.Role == YourRhythmRoles.Teacher
                    && item.IsActive,
                ct)
            ?? throw new UnauthorizedAccessException("Teacher account was not found.");

        return new TeacherAccountSettingsDto(
            user.DisplayName,
            user.Email,
            ToPublicPhotoUrl(user.ProfilePhotoPath));
    }

    public async Task<CredentialUpdateResult> UpdateStudentAccountAsync(
        AuthenticatedUserProfile profile,
        UpdateStudentAccountRequest request,
        CancellationToken ct = default)
    {
        RequireStudent(profile);
        var displayName = RequireText(request.DisplayName, "Nome");
        var contact = NormalizeOptionalContact(request.ExternalContact);

        var user = await _db.SchoolUsers
            .FirstOrDefaultAsync(
                item => item.Id == profile.SchoolUserId
                    && item.SchoolId == profile.SchoolId
                    && item.Role == YourRhythmRoles.Student
                    && item.IsActive,
                ct)
            ?? throw new UnauthorizedAccessException("Student account was not found.");

        var studentExists = await _db.StudentProfiles.AnyAsync(
            student => student.Id == profile.StudentProfileId
                && student.SchoolId == profile.SchoolId
                && student.SchoolUserId == user.Id,
            ct);
        if (!studentExists)
            throw new UnauthorizedAccessException("Student profile was not found.");

        user.DisplayName = displayName;
        user.Phone = contact;
        await UpdatePhotoAsync(user, request.Photo, request.RemovePhoto, ct);

        await _db.SaveChangesAsync(ct);
        return new CredentialUpdateResult(user.DisplayName, user.Email);
    }

    public async Task<CredentialUpdateResult> UpdateTeacherPhotoAsync(
        AuthenticatedUserProfile profile,
        UpdateTeacherPhotoRequest request,
        CancellationToken ct = default)
    {
        RequireTeacher(profile);
        var user = await GetTeacherSchoolUserForUpdateAsync(profile, ct);
        await UpdatePhotoAsync(user, request.Photo, request.RemovePhoto, ct);
        await _db.SaveChangesAsync(ct);
        return new CredentialUpdateResult(user.DisplayName, user.Email);
    }

    public async Task UpdateStudentPhotoByTeacherAsync(
        AuthenticatedUserProfile teacherProfile,
        Guid studentSchoolUserId,
        IFormFile photo,
        CancellationToken ct = default)
    {
        RequireTeacher(teacherProfile);

        var user = await _db.SchoolUsers
            .FirstOrDefaultAsync(
                u => u.Id == studentSchoolUserId
                    && u.SchoolId == teacherProfile.SchoolId
                    && u.Role == YourRhythmRoles.Student
                    && u.IsActive,
                ct)
            ?? throw new InvalidOperationException("Aluno nao encontrado.");

        await UpdatePhotoAsync(user, photo, removePhoto: false, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<CredentialUpdateResult> ChangeTeacherEmailAsync(
        AuthenticatedUserProfile profile,
        ChangeTeacherEmailRequest request,
        CancellationToken ct = default)
    {
        RequireTeacher(profile);
        var newEmail = NormalizeEmail(request.NewEmail);
        var account = await GetVerifiedTeacherAccountAsync(profile, request.CurrentPassword, ct);

        var existingAccount = await _accountStore.FindByEmailAsync(newEmail, ct);
        if (existingAccount is not null && existingAccount.Id != account.Id)
            throw new InvalidOperationException("Este e-mail ja esta em uso.");

        var user = await GetTeacherSchoolUserForUpdateAsync(profile, ct);
        var duplicateSchoolUser = await _db.SchoolUsers.AnyAsync(
            item => item.SchoolId == user.SchoolId
                && item.Email == newEmail
                && item.Id != user.Id,
            ct);
        if (duplicateSchoolUser)
            throw new InvalidOperationException("Este e-mail ja esta em uso nesta escola.");

        account.Email = newEmail;
        account.DisplayName = user.DisplayName;
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        await _accountStore.UpdateAsync(account, ct);

        user.Email = newEmail;
        await _db.SaveChangesAsync(ct);

        return new CredentialUpdateResult(user.DisplayName, user.Email);
    }

    public async Task<CredentialUpdateResult> ChangeTeacherPasswordAsync(
        AuthenticatedUserProfile profile,
        ChangeTeacherPasswordRequest request,
        CancellationToken ct = default)
    {
        RequireTeacher(profile);
        var account = await GetVerifiedTeacherAccountAsync(profile, request.CurrentPassword, ct);
        account.PasswordCredential = _passwordHasher.HashPassword(request.NewPassword);
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        await _accountStore.UpdateAsync(account, ct);

        var user = await GetTeacherSchoolUserForUpdateAsync(profile, ct);
        return new CredentialUpdateResult(user.DisplayName, user.Email);
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
        user.DisplayName = RequireText(displayName, "Nome");
        user.Phone = NormalizeOptionalContact(phone);
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

    private async Task<Domain.Users.SchoolUser> GetTeacherSchoolUserForUpdateAsync(
        AuthenticatedUserProfile profile,
        CancellationToken ct)
    {
        return await _db.SchoolUsers
            .FirstOrDefaultAsync(
                item => item.Id == profile.SchoolUserId
                    && item.SchoolId == profile.SchoolId
                    && item.Role == YourRhythmRoles.Teacher
                    && item.IsActive,
                ct)
            ?? throw new UnauthorizedAccessException("Teacher account was not found.");
    }

    private async Task<Foundation.Access.Accounts.Account> GetVerifiedTeacherAccountAsync(
        AuthenticatedUserProfile profile,
        string currentPassword,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentPassword))
            throw new UnauthorizedAccessException("Senha atual invalida.");

        var account = await _accountStore.FindByIdAsync(profile.AccountId, ct)
            ?? throw new UnauthorizedAccessException("Account was not found.");

        if (account.PasswordCredential is null)
            throw new UnauthorizedAccessException("Senha atual invalida.");

        var verification = _passwordHasher.Verify(currentPassword, account.PasswordCredential);
        if (!verification.IsSuccess)
            throw new UnauthorizedAccessException("Senha atual invalida.");

        if (verification.NeedsRehash)
        {
            account.PasswordCredential = _passwordHasher.HashPassword(currentPassword);
            await _accountStore.UpdateAsync(account, ct);
        }

        return account;
    }

    private async Task UpdatePhotoAsync(
        Domain.Users.SchoolUser user,
        IFormFile? photo,
        bool removePhoto,
        CancellationToken ct)
    {
        if (removePhoto)
        {
            DeleteExistingPhoto(user.ProfilePhotoPath);
            user.ProfilePhotoPath = null;
        }

        if (photo is null || photo.Length == 0)
            return;

        await ValidatePhotoAsync(photo, ct);
        var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
        var uploadRoot = ProfilePhotoRoot();
        Directory.CreateDirectory(uploadRoot);

        var fileName = $"profile-{user.Id:N}-{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadRoot, fileName);

        await using (var stream = File.Create(fullPath))
        {
            await photo.CopyToAsync(stream, ct);
        }

        DeleteExistingPhoto(user.ProfilePhotoPath);
        user.ProfilePhotoPath = $"uploads/profile-photos/{fileName}";
    }

    private async Task ValidatePhotoAsync(IFormFile photo, CancellationToken ct)
    {
        if (photo.Length > MaxPhotoBytes)
            throw new ArgumentException("A foto deve ter no maximo 5 MB.");

        var extension = Path.GetExtension(photo.FileName);
        if (!AllowedPhotoExtensions.Contains(extension))
            throw new ArgumentException("Envie uma foto JPG, PNG ou WebP.");

        await using var stream = photo.OpenReadStream();
        var buffer = new byte[12];
        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
        if (!HasValidImageSignature(buffer, read, extension))
            throw new ArgumentException("O arquivo enviado nao parece ser uma imagem valida.");
    }

    private static bool HasValidImageSignature(byte[] buffer, int read, string extension)
    {
        if (read < 4) return false;

        var isJpeg = read >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF;
        var isPng = read >= 8
            && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47
            && buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A;
        var isWebp = read >= 12
            && buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46
            && buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50;

        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            ? isJpeg
            : extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                ? isPng
                : extension.Equals(".webp", StringComparison.OrdinalIgnoreCase) && isWebp;
    }

    private void DeleteExistingPhoto(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        var uploadRoot = ProfilePhotoRoot();
        var fullPath = Path.GetFullPath(Path.Combine(_environment.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var allowedRoot = Path.GetFullPath(uploadRoot);

        if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
            return;

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    private string ProfilePhotoRoot()
        => Path.Combine(_environment.WebRootPath, "uploads", "profile-photos");

    private static string? ToPublicPhotoUrl(string? relativePath)
        => string.IsNullOrWhiteSpace(relativePath) ? null : "/" + relativePath.Replace('\\', '/');

    private static string RequireText(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{field} e obrigatorio.");

        return value.Trim();
    }

    private static string NormalizeEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Informe um e-mail valido.");

        try
        {
            var address = new MailAddress(value.Trim());
            return address.Address.ToUpperInvariant();
        }
        catch (FormatException)
        {
            throw new ArgumentException("Informe um e-mail valido.");
        }
    }

    private static string? NormalizeOptionalContact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Contains('@'))
            return NormalizeEmail(trimmed);

        var digits = trimmed.Count(char.IsDigit);
        if (digits < 7 || !PhoneRegex.IsMatch(trimmed))
            throw new ArgumentException("Contato deve ser um telefone ou e-mail valido.");

        return trimmed;
    }

    private static string LevelBadge(int level) => level switch
    {
        <= 1 => "Iniciante",
        2 => "Aprendiz",
        3 => "Intermediario",
        4 => "Avancado",
        _ => "Lendario"
    };

    private static void RequireStudent(AuthenticatedUserProfile profile)
    {
        if (profile.Role != YourRhythmRoles.Student
            || profile.SchoolId is null
            || profile.SchoolUserId is null
            || profile.StudentProfileId is null)
            throw new UnauthorizedAccessException("Student profile is required.");
    }

    private static void RequireTeacher(AuthenticatedUserProfile profile)
    {
        if (profile.Role != YourRhythmRoles.Teacher
            || profile.SchoolId is null
            || profile.SchoolUserId is null
            || profile.TeacherProfileId is null)
            throw new UnauthorizedAccessException("Teacher profile is required.");
    }
}
