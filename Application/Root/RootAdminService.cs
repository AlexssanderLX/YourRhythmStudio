using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Foundation.Access.Abstractions;
using Foundation.Access.Accounts;
using Foundation.Access.Security;
using Foundation.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Domain.Root;
using YourRhythmStudio.Domain.Users;
using YourRhythmStudio.Infrastructure.Data;
using YourRhythmStudio.Infrastructure.Email;
using YourRhythmStudio.ViewModels.Root;

namespace YourRhythmStudio.Application.Root;

public sealed class RootAdminService
{
    private readonly YourRhythmDbContext _db;
    private readonly IAccountStore _accountStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly IClock _clock;
    private readonly ILogger<RootAdminService> _logger;

    public RootAdminService(
        YourRhythmDbContext db,
        IAccountStore accountStore,
        IPasswordHasher passwordHasher,
        IEmailService email,
        IConfiguration config,
        IClock clock,
        ILogger<RootAdminService> logger)
    {
        _db = db;
        _accountStore = accountStore;
        _passwordHasher = passwordHasher;
        _email = email;
        _config = config;
        _clock = clock;
        _logger = logger;
    }

    // ──── Dashboard ────────────────────────────────────────────────────────────

    public async Task<RootDashboardViewModel> GetDashboardAsync(CancellationToken ct = default)
    {
        var accounts = await _db.PersistedAccounts.AsNoTracking().ToListAsync(ct);
        var pendingRequests = await _db.AccessRequests.AsNoTracking()
            .CountAsync(r => r.Status == AccessRequestStatus.Pending, ct);

        var schools = await _db.Schools.AsNoTracking().ToListAsync(ct);
        var planBreakdown = schools
            .GroupBy(s => s.PlanCode)
            .Select(g => new PlanSummaryRow { PlanCode = g.Key, PlanName = g.Key == "escola" ? "Escola" : "Professor", Count = g.Count() })
            .ToList();

        return new RootDashboardViewModel
        {
            TotalAccounts = accounts.Count,
            PendingAccounts = accounts.Count(a => a.Status == "PendingApproval"),
            ActiveAccounts = accounts.Count(a => a.Status == "Active"),
            BlockedAccounts = accounts.Count(a => a.Status == "Suspended"),
            CancelledAccounts = accounts.Count(a => a.Status == "Archived"),
            PendingRequests = pendingRequests,
            PlanBreakdown = planBreakdown
        };
    }

    // ──── Access Requests ──────────────────────────────────────────────────────

    public async Task<List<RequestRow>> GetRequestsAsync(string? statusFilter, CancellationToken ct = default)
    {
        var query = _db.AccessRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<AccessRequestStatus>(statusFilter, out var s))
            query = query.Where(r => r.Status == s);

        var rows = await query.OrderByDescending(r => r.CreatedAtUtc).ToListAsync(ct);

        return rows.Select(r => new RequestRow
        {
            Id = r.Id,
            ResponsibleName = r.ResponsibleName,
            Email = r.Email,
            SchoolName = r.SchoolName,
            PlanCode = r.PlanCode,
            Phone = r.Phone,
            Status = r.Status,
            CreatedAtUtc = r.CreatedAtUtc,
            ReviewedAtUtc = r.ReviewedAtUtc
        }).ToList();
    }

    public async Task<RequestDetailViewModel?> GetRequestAsync(Guid id, CancellationToken ct = default)
    {
        var r = await _db.AccessRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return null;

        return new RequestDetailViewModel
        {
            Id = r.Id,
            ResponsibleName = r.ResponsibleName,
            Email = r.Email,
            SchoolName = r.SchoolName,
            PlanCode = r.PlanCode,
            Phone = r.Phone,
            Status = r.Status,
            CreatedAtUtc = r.CreatedAtUtc,
            ReviewedAtUtc = r.ReviewedAtUtc,
            ReviewNote = r.ReviewNote,
            CreatedAccountId = r.CreatedAccountId
        };
    }

    public async Task<(bool Success, string? Error)> ApproveRequestAsync(
        Guid requestId, Guid actorId, string actorEmail, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var request = await _db.AccessRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
            if (request is null) return (false, "Solicitacao nao encontrada.");
            if (request.Status != AccessRequestStatus.Pending) return (false, "Solicitacao ja foi processada.");

            var now = _clock.UtcNow;
            var emailNorm = request.Email.ToUpperInvariant();
            var existing = await _accountStore.FindByEmailAsync(emailNorm, ct);
            if (existing is not null) return (false, "Ja existe uma conta com este e-mail.");

            var plan = await FindActivePlanAsync(request.PlanCode, ct);
            if (plan is null) return (false, "Plano da solicitacao nao esta ativo ou nao existe.");

            // Create account (PendingApproval until user sets password)
            var account = new Account
            {
                DisplayName = request.ResponsibleName,
                Email = emailNorm,
                Status = AccountStatus.PendingApproval,
                PlatformRole = PlatformAccessRole.None,
                CreatedAtUtc = now,
                SecurityStamp = Guid.NewGuid().ToString("N")
            };
            await _accountStore.SaveAsync(account, ct);

            // Create school/user/teacher
            var slug = GenerateSlug(request.SchoolName);
            if (await _db.Schools.AnyAsync(s => s.Slug == slug, ct))
                slug = slug + "-" + Guid.NewGuid().ToString("N")[..6];

            var school = new School
            {
                Name = request.SchoolName,
                Slug = slug,
                PrimaryEmail = request.Email,
                OwnerAccountId = account.Id,
                PlanCode = plan.Code,
                StorageQuotaBytes = plan.StorageQuotaBytes,
                CreatedAtUtc = now
            };
            _db.Schools.Add(school);

            var schoolUser = new SchoolUser
            {
                SchoolId = school.Id,
                AccountId = account.Id,
                DisplayName = request.ResponsibleName,
                Email = request.Email,
                Role = Domain.YourRhythmRoles.Teacher,
                Phone = request.Phone,
                CreatedAtUtc = now
            };
            _db.SchoolUsers.Add(schoolUser);

            var teacher = new TeacherProfile
            {
                SchoolId = school.Id,
                SchoolUserId = schoolUser.Id,
                CanManageStudents = true
            };
            _db.TeacherProfiles.Add(teacher);

            // Generate set-password token
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            request.Status = AccessRequestStatus.Approved;
            request.ReviewedAtUtc = now;
            request.ReviewedByAccountId = actorId;
            request.SetPasswordToken = token;
            request.SetPasswordTokenExpiresAtUtc = now.AddHours(72);
            request.CreatedAccountId = account.Id;

            _db.AdminAuditLogs.Add(new AdminAuditLog
            {
                ActorAccountId = actorId,
                ActorEmail = actorEmail,
                Action = "ApproveRequest",
                TargetType = "AccessRequest",
                TargetId = requestId.ToString(),
                Notes = $"Criou conta para {request.Email}",
                CreatedAtUtc = now
            });

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await SendApprovalEmailAsync(request, token, ct);

            return (true, null);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError("Erro ao aprovar solicitacao {Id}: {Type}", requestId, ex.GetType().Name);
            return (false, "Erro interno ao processar aprovacao.");
        }
    }

    public async Task<(bool Success, string? Error)> RejectRequestAsync(
        Guid requestId, Guid actorId, string actorEmail, string? note, CancellationToken ct = default)
    {
        var request = await _db.AccessRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
        if (request is null) return (false, "Solicitacao nao encontrada.");
        if (request.Status != AccessRequestStatus.Pending) return (false, "Solicitacao ja foi processada.");

        var now = _clock.UtcNow;
        request.Status = AccessRequestStatus.Rejected;
        request.ReviewedAtUtc = now;
        request.ReviewedByAccountId = actorId;
        request.ReviewNote = note;

        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            ActorAccountId = actorId,
            ActorEmail = actorEmail,
            Action = "RejectRequest",
            TargetType = "AccessRequest",
            TargetId = requestId.ToString(),
            Notes = note,
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    private async Task SendApprovalEmailAsync(AccessRequest request, string token, CancellationToken ct)
    {
        var baseUrl = _config["Application:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
        var link = $"{baseUrl}/Auth/SetPassword?token={token}";
        var html = $"""
            <h2>Sua conta YourRhythm foi aprovada!</h2>
            <p>Ola, <strong>{System.Net.WebUtility.HtmlEncode(request.ResponsibleName)}</strong>!</p>
            <p>Sua solicitacao de acesso foi aprovada. Clique no link abaixo para definir sua senha e acessar o sistema.</p>
            <p><a href="{link}" style="background:#2563EB;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:600">Definir minha senha</a></p>
            <p style="color:#64748b;font-size:12px">Este link expira em 72 horas. Se nao solicitou acesso, ignore este e-mail.</p>
            """;

        try
        {
            await _email.SendAsync(new EmailMessage
            {
                ToAddress = request.Email,
                ToName = request.ResponsibleName,
                Subject = "[YourRhythm] Sua conta foi aprovada — defina sua senha",
                HtmlBody = html
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Falha ao enviar e-mail de aprovacao para {Email}: {Type}", request.Email, ex.GetType().Name);
        }
    }

    // ──── SetPassword ──────────────────────────────────────────────────────────

    public async Task<AccessRequest?> FindBySetPasswordTokenAsync(string token, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        return await _db.AccessRequests.AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.SetPasswordToken == token &&
                r.SetPasswordTokenExpiresAtUtc > now &&
                r.Status == AccessRequestStatus.Approved, ct);
    }

    public async Task<(bool Success, string? Error)> SetPasswordAsync(string token, string password, CancellationToken ct = default)
    {
        var request = await _db.AccessRequests
            .FirstOrDefaultAsync(r => r.SetPasswordToken == token, ct);

        if (request is null) return (false, "Link invalido ou expirado.");
        if (request.SetPasswordTokenExpiresAtUtc <= _clock.UtcNow) return (false, "Link expirado. Solicite nova aprovacao ao administrador.");
        if (request.CreatedAccountId is null) return (false, "Conta nao encontrada.");

        var emailNorm = request.Email.ToUpperInvariant();
        var account = await _accountStore.FindByEmailAsync(emailNorm, ct);
        if (account is null) return (false, "Conta nao encontrada.");

        account.PasswordCredential = _passwordHasher.HashPassword(password);
        account.Status = AccountStatus.Active;
        account.ActivatedAtUtc = _clock.UtcNow;
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        await _accountStore.UpdateAsync(account, ct);

        // Invalidate token
        request.SetPasswordToken = null;
        request.SetPasswordTokenExpiresAtUtc = null;
        await _db.SaveChangesAsync(ct);

        return (true, null);
    }

    // ──── Accounts ─────────────────────────────────────────────────────────────

    public async Task<List<AccountRow>> GetAccountsAsync(string? search, string? statusFilter, string? planFilter, CancellationToken ct = default)
    {
        var accounts = await _db.PersistedAccounts.AsNoTracking().ToListAsync(ct);
        var schools = await _db.Schools.AsNoTracking().ToListAsync(ct);

        var joined = from a in accounts
                     join s in schools on a.Id equals (s.OwnerAccountId ?? Guid.Empty) into gs
                     from s in gs.DefaultIfEmpty()
                     select new AccountRow
                     {
                         AccountId = a.Id,
                         SchoolId = s?.Id,
                         DisplayName = a.DisplayName,
                         Email = a.Email.ToLowerInvariant(),
                         SchoolName = s?.Name ?? "—",
                         PlanCode = s?.PlanCode ?? "—",
                         Status = a.Status,
                         CreatedAtUtc = a.CreatedAtUtc,
                         StorageUsedBytes = s?.StorageUsedBytes ?? 0,
                         StorageQuotaBytes = s?.StorageQuotaBytes ?? 0
                     };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLowerInvariant();
            joined = joined.Where(a =>
                a.Email.Contains(q) ||
                a.DisplayName.ToLowerInvariant().Contains(q) ||
                a.SchoolName.ToLowerInvariant().Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
            joined = joined.Where(a => a.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(planFilter))
            joined = joined.Where(a => a.PlanCode.Equals(planFilter, StringComparison.OrdinalIgnoreCase));

        return joined.OrderByDescending(a => a.CreatedAtUtc).ToList();
    }

    public async Task<AccountDetailViewModel?> GetAccountDetailAsync(Guid accountId, CancellationToken ct = default)
    {
        var account = await _accountStore.FindByIdAsync(accountId, ct);
        if (account is null) return null;

        var school = await _db.Schools.AsNoTracking()
            .FirstOrDefaultAsync(s => s.OwnerAccountId == accountId, ct);

        var studentCount = school is null ? 0 : await _db.StudentProfiles.AsNoTracking()
            .CountAsync(s => s.SchoolId == school.Id, ct);
        var teacherCount = school is null ? 0 : await _db.TeacherProfiles.AsNoTracking()
            .CountAsync(t => t.SchoolId == school.Id, ct);

        var logs = await _db.AdminAuditLogs.AsNoTracking()
            .Where(l => l.TargetId == accountId.ToString())
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(20)
            .Select(l => new AdminAuditLogRow { ActorEmail = l.ActorEmail, Action = l.Action, Notes = l.Notes, CreatedAtUtc = l.CreatedAtUtc })
            .ToListAsync(ct);

        return new AccountDetailViewModel
        {
            AccountId = account.Id,
            SchoolId = school?.Id,
            DisplayName = account.DisplayName,
            Email = account.Email.ToLowerInvariant(),
            SchoolName = school?.Name ?? "—",
            PlanCode = school?.PlanCode ?? "—",
            Status = account.Status.ToString(),
            CreatedAtUtc = account.CreatedAtUtc,
            ActivatedAtUtc = account.ActivatedAtUtc,
            StorageUsedBytes = school?.StorageUsedBytes ?? 0,
            StorageQuotaBytes = school?.StorageQuotaBytes ?? 0,
            StudentCount = studentCount,
            TeacherCount = teacherCount,
            AuditLogs = logs
        };
    }

    public async Task<(bool Success, string? Error)> CreateAccountAsync(CreateAccountViewModel vm, Guid actorId, string actorEmail, CancellationToken ct = default)
    {
        var emailNorm = vm.Email.Trim().ToUpperInvariant();
        var existing = await _accountStore.FindByEmailAsync(emailNorm, ct);
        if (existing is not null) return (false, "E-mail ja em uso.");

        var plan = await FindActivePlanAsync(vm.PlanCode, ct);
        if (plan is null) return (false, "Plano invalido ou inativo.");

        var now = _clock.UtcNow;
        var account = new Account
        {
            DisplayName = vm.DisplayName.Trim(),
            Email = emailNorm,
            Status = AccountStatus.Active,
            PlatformRole = PlatformAccessRole.None,
            PasswordCredential = _passwordHasher.HashPassword(vm.Password),
            CreatedAtUtc = now,
            ActivatedAtUtc = now,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        await _accountStore.SaveAsync(account, ct);

        var slug = GenerateSlug(vm.SchoolName);
        if (await _db.Schools.AnyAsync(s => s.Slug == slug, ct))
            slug = slug + "-" + Guid.NewGuid().ToString("N")[..6];

        var school = new School
        {
            Name = vm.SchoolName.Trim(),
            Slug = slug,
            PrimaryEmail = vm.Email.Trim().ToLowerInvariant(),
            OwnerAccountId = account.Id,
            PlanCode = plan.Code,
            StorageQuotaBytes = plan.StorageQuotaBytes,
            CreatedAtUtc = now
        };
        _db.Schools.Add(school);

        var schoolUser = new SchoolUser
        {
            SchoolId = school.Id,
            AccountId = account.Id,
            DisplayName = vm.DisplayName.Trim(),
            Email = vm.Email.Trim().ToLowerInvariant(),
            Role = Domain.YourRhythmRoles.Teacher,
            Phone = vm.Phone,
            CreatedAtUtc = now
        };
        _db.SchoolUsers.Add(schoolUser);

        _db.TeacherProfiles.Add(new TeacherProfile { SchoolId = school.Id, SchoolUserId = schoolUser.Id, CanManageStudents = true });

        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            ActorAccountId = actorId, ActorEmail = actorEmail,
            Action = "CreateAccount", TargetType = "Account", TargetId = account.Id.ToString(),
            Notes = $"Criou conta manualmente para {account.Email}", CreatedAtUtc = now
        });

        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> EditAccountAsync(EditAccountViewModel vm, Guid actorId, string actorEmail, CancellationToken ct = default)
    {
        var account = await _accountStore.FindByIdAsync(vm.AccountId, ct);
        if (account is null) return (false, "Conta nao encontrada.");

        if (account.PlatformRole == PlatformAccessRole.PlatformAdmin)
            return (false, "Conta Root nao pode ser editada por este painel.");

        var plan = await FindActivePlanAsync(vm.PlanCode, ct);
        if (plan is null) return (false, "Plano invalido ou inativo.");

        account.DisplayName = vm.DisplayName.Trim();
        await _accountStore.UpdateAsync(account, ct);

        var school = await _db.Schools.FirstOrDefaultAsync(s => s.OwnerAccountId == vm.AccountId, ct);
        if (school is not null)
        {
            school.Name = vm.SchoolName.Trim();
            school.PlanCode = plan.Code;
            school.StorageQuotaBytes = plan.StorageQuotaBytes;
        }

        var schoolUser = await _db.SchoolUsers.FirstOrDefaultAsync(u => u.AccountId == vm.AccountId, ct);
        if (schoolUser is not null)
        {
            schoolUser.DisplayName = vm.DisplayName.Trim();
            schoolUser.Phone = vm.Phone;
        }

        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            ActorAccountId = actorId, ActorEmail = actorEmail,
            Action = "EditAccount", TargetType = "Account", TargetId = vm.AccountId.ToString(),
            Notes = $"Editou dados da conta", CreatedAtUtc = _clock.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> BlockAccountAsync(Guid accountId, Guid actorId, string actorEmail, CancellationToken ct = default)
    {
        var account = await _accountStore.FindByIdAsync(accountId, ct);
        if (account is null) return (false, "Conta nao encontrada.");
        if (account.PlatformRole == PlatformAccessRole.PlatformAdmin) return (false, "Conta Root nao pode ser bloqueada.");

        account.Status = AccountStatus.Suspended;
        await _accountStore.UpdateAsync(account, ct);

        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            ActorAccountId = actorId, ActorEmail = actorEmail,
            Action = "BlockAccount", TargetType = "Account", TargetId = accountId.ToString(),
            CreatedAtUtc = _clock.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UnblockAccountAsync(Guid accountId, Guid actorId, string actorEmail, CancellationToken ct = default)
    {
        var account = await _accountStore.FindByIdAsync(accountId, ct);
        if (account is null) return (false, "Conta nao encontrada.");

        account.Status = AccountStatus.Active;
        account.ActivatedAtUtc ??= _clock.UtcNow;
        await _accountStore.UpdateAsync(account, ct);

        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            ActorAccountId = actorId, ActorEmail = actorEmail,
            Action = "UnblockAccount", TargetType = "Account", TargetId = accountId.ToString(),
            CreatedAtUtc = _clock.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> CancelAccountAsync(Guid accountId, Guid actorId, string actorEmail, CancellationToken ct = default)
    {
        var account = await _accountStore.FindByIdAsync(accountId, ct);
        if (account is null) return (false, "Conta nao encontrada.");
        if (account.PlatformRole == PlatformAccessRole.PlatformAdmin) return (false, "Conta Root nao pode ser cancelada.");

        account.Status = AccountStatus.Archived;
        await _accountStore.UpdateAsync(account, ct);

        var school = await _db.Schools.FirstOrDefaultAsync(s => s.OwnerAccountId == accountId, ct);
        if (school is not null) school.IsActive = false;

        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            ActorAccountId = actorId, ActorEmail = actorEmail,
            Action = "CancelAccount", TargetType = "Account", TargetId = accountId.ToString(),
            CreatedAtUtc = _clock.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    // ──── Plans ────────────────────────────────────────────────────────────────

    public async Task<List<PlanRow>> GetPlansAsync(CancellationToken ct = default)
    {
        var plans = await _db.Plans.AsNoTracking().OrderBy(p => p.IsActive ? 0 : 1).ThenBy(p => p.Name).ToListAsync(ct);
        var schoolCounts = await _db.Schools.AsNoTracking()
            .GroupBy(s => s.PlanCode)
            .Select(g => new { Code = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Code, g => g.Count, ct);

        return plans.Select(p => new PlanRow
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Description = p.Description,
            MonthlyPriceBrl = p.MonthlyPriceBrl,
            MaxStudents = p.MaxStudents,
            StorageQuotaBytes = p.StorageQuotaBytes,
            IsActive = p.IsActive,
            ActiveSchools = schoolCounts.GetValueOrDefault(p.Code, 0)
        }).ToList();
    }

    public async Task<(bool Success, string? Error)> UpsertPlanAsync(UpsertPlanViewModel vm, Guid actorId, string actorEmail, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        Plan? plan;

        if (vm.Id.HasValue)
        {
            plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == vm.Id, ct);
            if (plan is null) return (false, "Plano nao encontrado.");

            if (!plan.IsActive && vm.IsActive == false)
            {
                // Allowing deactivation — no issue
            }

            plan.Name = vm.Name.Trim();
            plan.Description = vm.Description?.Trim();
            plan.MonthlyPriceBrl = vm.MonthlyPriceBrl;
            plan.MaxStudents = vm.MaxStudentsInput > 0 ? vm.MaxStudentsInput : null;
            plan.StorageQuotaBytes = (long)vm.StorageQuotaGb * 1024 * 1024 * 1024;
            plan.IsActive = vm.IsActive;

            _db.AdminAuditLogs.Add(new AdminAuditLog
            {
                ActorAccountId = actorId, ActorEmail = actorEmail,
                Action = "EditPlan", TargetType = "Plan", TargetId = plan.Id.ToString(),
                Notes = $"Atualizou plano {plan.Code}", CreatedAtUtc = now
            });
        }
        else
        {
            var codeExists = await _db.Plans.AnyAsync(p => p.Code == vm.Code.Trim(), ct);
            if (codeExists) return (false, "Codigo de plano ja existe.");

            plan = new Plan
            {
                Code = vm.Code.Trim().ToLowerInvariant(),
                Name = vm.Name.Trim(),
                Description = vm.Description?.Trim(),
                MonthlyPriceBrl = vm.MonthlyPriceBrl,
                MaxStudents = vm.MaxStudentsInput > 0 ? vm.MaxStudentsInput : null,
                StorageQuotaBytes = (long)vm.StorageQuotaGb * 1024 * 1024 * 1024,
                IsActive = vm.IsActive
            };
            _db.Plans.Add(plan);

            _db.AdminAuditLogs.Add(new AdminAuditLog
            {
                ActorAccountId = actorId, ActorEmail = actorEmail,
                Action = "CreatePlan", TargetType = "Plan", TargetId = plan.Id.ToString(),
                Notes = $"Criou plano {plan.Code}", CreatedAtUtc = now
            });
        }

        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    // ──── Storage ──────────────────────────────────────────────────────────────

    private async Task<Plan?> FindActivePlanAsync(string planCode, CancellationToken ct)
    {
        var normalized = planCode.Trim().ToLowerInvariant();
        if (normalized is not ("professor" or "escola"))
            return null;

        return await _db.Plans.AsNoTracking()
            .FirstOrDefaultAsync(plan => plan.Code == normalized && plan.IsActive, ct);
    }

    public async Task<List<StorageRow>> GetStorageOverviewAsync(CancellationToken ct = default)
    {
        var schools = await _db.Schools.AsNoTracking().Include(s => s.Users).ToListAsync(ct);
        return schools.Select(s => new StorageRow
        {
            SchoolId = s.Id,
            SchoolName = s.Name,
            OwnerEmail = s.PrimaryEmail,
            UsedBytes = s.StorageUsedBytes,
            QuotaBytes = s.StorageQuotaBytes
        }).OrderByDescending(r => r.QuotaBytes > 0 ? (double)r.UsedBytes / r.QuotaBytes : 0).ToList();
    }

    public async Task<(bool Success, string? Error)> UpdateStorageQuotaAsync(
        Guid schoolId, int quotaGb, Guid actorId, string actorEmail, CancellationToken ct = default)
    {
        if (quotaGb < 1 || quotaGb > 10000) return (false, "Cota invalida (1-10000 GB).");

        var school = await _db.Schools.FirstOrDefaultAsync(s => s.Id == schoolId, ct);
        if (school is null) return (false, "Escola nao encontrada.");

        school.StorageQuotaBytes = (long)quotaGb * 1024 * 1024 * 1024;

        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            ActorAccountId = actorId, ActorEmail = actorEmail,
            Action = "UpdateStorageQuota", TargetType = "School", TargetId = schoolId.ToString(),
            Notes = $"Cota alterada para {quotaGb} GB", CreatedAtUtc = _clock.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    // ──── Admin Settings ───────────────────────────────────────────────────────

    public async Task<string?> GetSettingAsync(string key, CancellationToken ct = default)
    {
        var setting = await _db.AdminSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key, ct);
        return setting?.Value;
    }

    public async Task SaveSettingAsync(string key, string? value, CancellationToken ct = default)
    {
        var setting = await _db.AdminSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (setting is null)
        {
            _db.AdminSettings.Add(new Domain.Root.AdminSetting { Key = key, Value = value, UpdatedAtUtc = _clock.UtcNow });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAtUtc = _clock.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<RootSettingsViewModel> GetSettingsViewModelAsync(Guid rootAccountId, CancellationToken ct = default)
    {
        var account = await _accountStore.FindByIdAsync(rootAccountId, ct);
        var storedRecipient = await GetSettingAsync(AdminSettingKeys.NotificationRecipient, ct);
        var configRecipient = _config["Email:AdminNotificationRecipient"];
        var recipient = !string.IsNullOrWhiteSpace(storedRecipient)
            ? storedRecipient
            : configRecipient;

        var smtpHost = _config["Email:Smtp:Host"]?.Trim() ?? string.Empty;
        var smtpPort = _config["Email:Smtp:Port"]?.Trim() ?? string.Empty;
        var smtpSender = _config["Email:Smtp:SenderEmail"]?.Trim() ?? string.Empty;
        var smtpUsername = _config["Email:Smtp:Username"]?.Trim() ?? string.Empty;
        var hasPassword = !string.IsNullOrWhiteSpace(_config["Email:Smtp:Password"]);
        var smtpReady = !string.IsNullOrWhiteSpace(smtpHost)
            && !string.IsNullOrWhiteSpace(smtpSender);
        var notificationReady = smtpReady && !string.IsNullOrWhiteSpace(recipient);

        return new RootSettingsViewModel
        {
            CurrentEmail = account?.Email.ToLowerInvariant() ?? string.Empty,
            NotificationRecipient = recipient ?? string.Empty,
            NotificationRecipientSource = !string.IsNullOrWhiteSpace(storedRecipient)
                ? "Banco de dados"
                : !string.IsNullOrWhiteSpace(configRecipient)
                    ? "Configuracao local"
                    : "Nao configurado",
            SmtpHost = smtpHost,
            SmtpPort = string.IsNullOrWhiteSpace(smtpPort) ? "587" : smtpPort,
            SmtpSenderEmail = smtpSender,
            SmtpUsername = smtpUsername,
            HasSmtpPassword = hasPassword,
            IsSmtpReady = smtpReady,
            IsNotificationReady = notificationReady,
            EmailStatusMessage = notificationReady
                ? "SMTP e destinatario configurados para envio de notificacoes."
                : "Configure SMTP e destinatario para enviar notificacoes reais."
        };
    }

    public async Task<(bool Success, string? Error)> SendTestNotificationEmailAsync(
        Guid rootAccountId,
        CancellationToken ct = default)
    {
        var settings = await GetSettingsViewModelAsync(rootAccountId, ct);
        if (!settings.IsNotificationReady)
        {
            return (false, "SMTP ou destinatario ainda nao estao configurados.");
        }

        try
        {
            await _email.SendAsync(new EmailMessage
            {
                ToAddress = settings.NotificationRecipient,
                ToName = "Admin YourRhythm",
                Subject = "[YourRhythm] Teste de e-mail",
                HtmlBody = """
                    <h2>Teste de e-mail do YourRhythm Studio</h2>
                    <p>Se voce recebeu esta mensagem, as notificacoes administrativas estao configuradas.</p>
                    <p>Nenhuma senha ou dado sensivel foi incluido neste teste.</p>
                    """
            }, ct);

            _db.AdminAuditLogs.Add(new AdminAuditLog
            {
                ActorAccountId = rootAccountId,
                ActorEmail = settings.CurrentEmail,
                Action = "SendTestNotificationEmail",
                TargetType = "Email",
                TargetId = settings.NotificationRecipient,
                Notes = "Enviou e-mail de teste de configuracao",
                CreatedAtUtc = _clock.UtcNow
            });
            await _db.SaveChangesAsync(ct);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError("Falha ao enviar e-mail de teste para {Recipient}: {Type}", settings.NotificationRecipient, ex.GetType().Name);
            return (false, "Nao foi possivel enviar o e-mail de teste. Confira SMTP, senha de app e permissao do provedor.");
        }
    }

    public async Task<(bool Success, string? Error)> UpdateRootCredentialsAsync(
        Guid rootAccountId, string newEmail, string? newPassword, string currentPassword, CancellationToken ct = default)
    {
        var account = await _accountStore.FindByIdAsync(rootAccountId, ct);
        if (account is null) return (false, "Conta Root nao encontrada.");
        if (account.PasswordCredential is null) return (false, "Conta Root sem credencial de senha configurada.");

        // Verifica senha atual
        if (!_passwordHasher.Verify(currentPassword, account.PasswordCredential).IsSuccess)
            return (false, "Senha atual incorreta.");

        var emailNorm = newEmail.Trim().ToUpperInvariant();

        // Verifica conflito de email apenas se mudou
        if (emailNorm != account.Email)
        {
            var conflict = await _accountStore.FindByEmailAsync(emailNorm, ct);
            if (conflict is not null && conflict.Id != rootAccountId)
                return (false, "Este e-mail ja esta em uso.");

            account.Email = emailNorm;
        }

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            if (newPassword.Length < 8)
                return (false, "A nova senha deve ter pelo menos 8 caracteres.");
            account.PasswordCredential = _passwordHasher.HashPassword(newPassword);
            account.SecurityStamp = Guid.NewGuid().ToString("N");
        }

        await _accountStore.UpdateAsync(account, ct);

        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            ActorAccountId = rootAccountId,
            ActorEmail = account.Email.ToLowerInvariant(),
            Action = "UpdateRootCredentials",
            TargetType = "Account",
            TargetId = rootAccountId.ToString(),
            Notes = "Credenciais Root atualizadas",
            CreatedAtUtc = _clock.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        return (true, null);
    }

    // ──── Landing Soundtrack ───────────────────────────────────────────────────

    public const int MaxTracks = 10;
    private static readonly string[] AllowedExtensions = [".mp3", ".ogg", ".wav", ".flac"];

    public async Task<List<Domain.Root.LandingTrack>> GetTracksAsync(CancellationToken ct = default) =>
        await _db.LandingTracks.AsNoTracking().OrderBy(t => t.SortOrder).ThenBy(t => t.UploadedAtUtc).ToListAsync(ct);

    public async Task<(bool Success, string? Error)> AddTrackAsync(
        string title, IFormFile file, string uploadDir, CancellationToken ct = default)
    {
        var count = await _db.LandingTracks.CountAsync(ct);
        if (count >= MaxTracks) return (false, $"Limite de {MaxTracks} musicas atingido.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext)) return (false, "Formato invalido. Use mp3, ogg, wav ou flac.");

        if (file.Length > 20 * 1024 * 1024) return (false, "Arquivo muito grande. Limite: 20 MB.");

        Directory.CreateDirectory(uploadDir);
        var safeFileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadDir, safeFileName);

        await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, ct);

        var nextOrder = count == 0 ? 0 : await _db.LandingTracks.MaxAsync(t => t.SortOrder, ct) + 1;
        _db.LandingTracks.Add(new Domain.Root.LandingTrack
        {
            Title = title.Trim(),
            FileName = safeFileName,
            SortOrder = nextOrder
        });
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemoveTrackAsync(
        Guid trackId, string uploadDir, CancellationToken ct = default)
    {
        var track = await _db.LandingTracks.FirstOrDefaultAsync(t => t.Id == trackId, ct);
        if (track is null) return (false, "Musica nao encontrada.");

        var fullPath = Path.Combine(uploadDir, track.FileName);
        if (File.Exists(fullPath)) File.Delete(fullPath);

        _db.LandingTracks.Remove(track);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    private static string GenerateSlug(string name)
    {
        var s = name.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"[^a-z0-9\s-]", "");
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, @"-+", "-").Trim('-');
        return s.Length > 80 ? s[..80] : s;
    }
}
