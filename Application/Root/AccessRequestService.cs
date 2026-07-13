using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Domain.Root;
using YourRhythmStudio.Infrastructure.Data;
using YourRhythmStudio.Infrastructure.Email;
using YourRhythmStudio.ViewModels.Auth;

namespace YourRhythmStudio.Application.Root;

public sealed class AccessRequestService
{
    private static readonly string[] RequestablePlanCodes = ["professor", "escola"];

    private readonly YourRhythmDbContext _db;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly ILogger<AccessRequestService> _logger;

    public AccessRequestService(
        YourRhythmDbContext db,
        IEmailService email,
        IConfiguration config,
        ILogger<AccessRequestService> logger)
    {
        _db = db;
        _email = email;
        _config = config;
        _logger = logger;
    }

    public async Task<RegisterViewModel> BuildRegisterViewModelAsync(
        string? selectedPlanCode = null,
        CancellationToken ct = default)
    {
        return new RegisterViewModel
        {
            PlanCode = NormalizePlanCode(selectedPlanCode),
            PlanOptions = await GetRequestablePlanOptionsAsync(ct)
        };
    }

    public async Task<IReadOnlyCollection<RegisterPlanOptionViewModel>> GetRequestablePlanOptionsAsync(CancellationToken ct = default)
    {
        return await _db.Plans
            .AsNoTracking()
            .Where(plan => plan.IsActive && (plan.Code == "professor" || plan.Code == "escola"))
            .OrderBy(plan => plan.Code == "professor" ? 0 : 1)
            .ThenBy(plan => plan.Name)
            .Select(plan => new RegisterPlanOptionViewModel
            {
                Code = plan.Code,
                Name = plan.Name,
                Description = plan.Description ?? string.Empty,
                MonthlyPriceBrl = plan.MonthlyPriceBrl,
                MaxStudents = plan.MaxStudents
            })
            .ToListAsync(ct);
    }

    public async Task<SubmitAccessRequestResult> SubmitAsync(RegisterViewModel model, CancellationToken ct = default)
    {
        var planCode = NormalizePlanCode(model.PlanCode);
        var plan = await FindRequestablePlanAsync(planCode, ct);
        if (plan is null)
            return SubmitAccessRequestResult.Failed("Escolha um plano valido para continuar.", nameof(RegisterViewModel.PlanCode));

        var emailNorm = model.Email.Trim().ToLowerInvariant();

        var duplicatePendingRequest = await _db.AccessRequests
            .AsNoTracking()
            .AnyAsync(request => request.Email == emailNorm && request.Status == AccessRequestStatus.Pending, ct);
        if (duplicatePendingRequest)
            return SubmitAccessRequestResult.Duplicate();

        var existingActiveUser = await _db.SchoolUsers
            .AsNoTracking()
            .AnyAsync(user => user.Email == emailNorm && user.IsActive, ct);
        if (existingActiveUser)
            return SubmitAccessRequestResult.Duplicate();

        var request = new AccessRequest
        {
            PlanCode = plan.Code,
            ResponsibleName = model.DisplayName.Trim(),
            Email = emailNorm,
            SchoolName = plan.Code == "escola"
                ? model.SchoolName!.Trim()
                : model.DisplayName.Trim(),
            Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim()
        };

        _db.AccessRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        var notificationSent = await SendNotificationAsync(request, plan, ct);

        return SubmitAccessRequestResult.Created(request.Id, notificationSent);
    }

    private async Task<Plan?> FindRequestablePlanAsync(string planCode, CancellationToken ct)
    {
        if (!RequestablePlanCodes.Contains(planCode))
            return null;

        return await _db.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(plan => plan.Code == planCode && plan.IsActive, ct);
    }

    private async Task<string?> GetNotificationRecipientAsync(CancellationToken ct)
    {
        var setting = await _db.AdminSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Key == AdminSettingKeys.NotificationRecipient, ct);

        if (!string.IsNullOrWhiteSpace(setting?.Value))
            return setting.Value;

        return _config["Email:AdminNotificationRecipient"];
    }

    private async Task<bool> SendNotificationAsync(AccessRequest request, Plan plan, CancellationToken ct)
    {
        var recipient = await GetNotificationRecipientAsync(ct);
        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning("Access request notification recipient is not configured.");
            return false;
        }

        var html = $"""
            <h2>Nova solicitacao de acesso - YourRhythm Studio</h2>
            <table>
              <tr><td><strong>Nome:</strong></td><td>{System.Net.WebUtility.HtmlEncode(request.ResponsibleName)}</td></tr>
              <tr><td><strong>E-mail:</strong></td><td>{System.Net.WebUtility.HtmlEncode(request.Email)}</td></tr>
              <tr><td><strong>Escola/Studio:</strong></td><td>{System.Net.WebUtility.HtmlEncode(request.SchoolName)}</td></tr>
              <tr><td><strong>Contato:</strong></td><td>{System.Net.WebUtility.HtmlEncode(request.Phone ?? "-")}</td></tr>
              <tr><td><strong>Plano:</strong></td><td>{System.Net.WebUtility.HtmlEncode(plan.Name)} ({System.Net.WebUtility.HtmlEncode(plan.Code)})</td></tr>
              <tr><td><strong>Data:</strong></td><td>{request.CreatedAtUtc:dd/MM/yyyy HH:mm} UTC</td></tr>
            </table>
            <p>Acesse o painel Root para aprovar ou rejeitar.</p>
            """;

        try
        {
            await _email.SendAsync(new EmailMessage
            {
                ToAddress = recipient,
                ToName = "Admin YourRhythm",
                Subject = $"[YourRhythm] Nova solicitacao de acesso - {request.ResponsibleName}",
                HtmlBody = html
            }, ct);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Access request notification failed for request {RequestId}: {Type}", request.Id, ex.GetType().Name);
            return false;
        }
    }

    private static string NormalizePlanCode(string? value)
        => (value ?? string.Empty).Trim().ToLowerInvariant();
}

public sealed record SubmitAccessRequestResult(
    bool Success,
    bool IsDuplicate,
    bool NotificationSent,
    Guid? RequestId,
    string? Error,
    string? MemberName)
{
    public static SubmitAccessRequestResult Created(Guid requestId, bool notificationSent)
        => new(true, false, notificationSent, requestId, null, null);

    public static SubmitAccessRequestResult Duplicate()
        => new(true, true, false, null, null, null);

    public static SubmitAccessRequestResult Failed(string error, string? memberName = null)
        => new(false, false, false, null, error, memberName);
}
