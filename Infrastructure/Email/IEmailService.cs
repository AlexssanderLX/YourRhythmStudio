namespace YourRhythmStudio.Infrastructure.Email;

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}

public sealed class EmailMessage
{
    public required string ToAddress { get; init; }
    public required string ToName { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
}
