using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace YourRhythmStudio.Infrastructure.Email;

public sealed class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var host = _config["Email:Smtp:Host"];
        var portStr = _config["Email:Smtp:Port"];
        var useSsl = _config.GetValue<bool>("Email:Smtp:UseSsl");
        var username = _config["Email:Smtp:Username"];
        var password = _config["Email:Smtp:Password"];
        var senderEmail = _config["Email:Smtp:SenderEmail"];
        var senderName = _config["Email:Smtp:SenderName"] ?? "YourRhythm Studio";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(senderEmail))
        {
            _logger.LogWarning("E-mail nao configurado (Email:Smtp:Host ou Email:Smtp:SenderEmail ausente). Mensagem para {To} nao enviada.", message.ToAddress);
            return;
        }

        var port = int.TryParse(portStr, out var p) ? p : 587;

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(senderName, senderEmail));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToAddress));
        mime.Subject = message.Subject;
        mime.Body = new TextPart("html") { Text = message.HtmlBody };

        using var client = new SmtpClient();
        try
        {
            var socketOptions = useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
            await client.ConnectAsync(host, port, socketOptions, ct);

            if (!string.IsNullOrWhiteSpace(username))
                await client.AuthenticateAsync(username, password, ct);

            await client.SendAsync(mime, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError("Falha ao enviar e-mail para {To}: {Type}", message.ToAddress, ex.GetType().Name);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
    }
}
