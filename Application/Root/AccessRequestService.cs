using Microsoft.EntityFrameworkCore;
using YourRhythmStudio.Domain.Root;
using YourRhythmStudio.Infrastructure.Data;
using YourRhythmStudio.Infrastructure.Email;
using YourRhythmStudio.ViewModels.Auth;

namespace YourRhythmStudio.Application.Root;

public sealed class AccessRequestService
{
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

    private async Task<string?> GetNotificationRecipientAsync(CancellationToken ct)
    {
        // Prioridade: 1. DB settings  2. Config/env var
        var setting = await _db.AdminSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == AdminSettingKeys.NotificationRecipient, ct);
        if (!string.IsNullOrWhiteSpace(setting?.Value)) return setting.Value;
        return _config["Email:AdminNotificationRecipient"];
    }

    public async Task<bool> SubmitAsync(RegisterViewModel model, CancellationToken ct = default)
    {
        var emailNorm = model.Email.Trim().ToLowerInvariant();

        // Silently accept if duplicate — don't reveal whether account/request exists
        var duplicate = await _db.AccessRequests
            .AsNoTracking()
            .AnyAsync(r => r.Email == emailNorm && r.Status == AccessRequestStatus.Pending, ct);
        if (duplicate) return true;

        var existingAccount = await _db.SchoolUsers
            .AsNoTracking()
            .AnyAsync(u => u.Email == emailNorm && u.IsActive, ct);
        if (existingAccount) return true;

        var request = new AccessRequest
        {
            PlanCode = model.PlanCode,
            ResponsibleName = model.DisplayName.Trim(),
            Email = emailNorm,
            SchoolName = model.SchoolName.Trim(),
            Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim()
        };

        _db.AccessRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        await SendNotificationAsync(request, ct);

        return true;
    }

    private async Task SendNotificationAsync(AccessRequest request, CancellationToken ct)
    {
        var recipient = await GetNotificationRecipientAsync(ct);
        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning("Destinatario de notificacao nao configurado. Configure em /Root/Settings ou Email:AdminNotificationRecipient.");
            return;
        }

        var planLabel = request.PlanCode == "escola" ? "Escola" : "Professor";
        var html = $"""
            <h2>Nova solicitacao de acesso — YourRhythm Studio</h2>
            <table>
              <tr><td><strong>Nome:</strong></td><td>{System.Net.WebUtility.HtmlEncode(request.ResponsibleName)}</td></tr>
              <tr><td><strong>E-mail:</strong></td><td>{System.Net.WebUtility.HtmlEncode(request.Email)}</td></tr>
              <tr><td><strong>Escola/Studio:</strong></td><td>{System.Net.WebUtility.HtmlEncode(request.SchoolName)}</td></tr>
              <tr><td><strong>Contato:</strong></td><td>{System.Net.WebUtility.HtmlEncode(request.Phone ?? "—")}</td></tr>
              <tr><td><strong>Plano:</strong></td><td>{planLabel}</td></tr>
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
                Subject = $"[YourRhythm] Nova solicitacao de acesso — {request.ResponsibleName}",
                HtmlBody = html
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Falha ao enviar notificacao de solicitacao {Id}: {Type}", request.Id, ex.GetType().Name);
            // Solicitacao ja foi salva — falha no e-mail nao a descarta
        }
    }
}
