using Foundation.Access.Abstractions;
using Foundation.Access.Models;
using Foundation.Access.Options;
using Foundation.Core.Abstractions;
using Foundation.Core.Models;
using Foundation.Core.Utilities;

namespace Foundation.Access.Services;

public sealed class AccessService
{
    private readonly AccessModuleOptions _options;
    private readonly IAccessChallengeStore _challengeStore;
    private readonly ISessionTicketStore _sessionStore;
    private readonly IAccessMessageSender _messageSender;
    private readonly IClock _clock;

    public AccessService(
        AccessModuleOptions options,
        IAccessChallengeStore challengeStore,
        ISessionTicketStore sessionStore,
        IAccessMessageSender messageSender,
        IClock clock)
    {
        _options = options;
        _challengeStore = challengeStore;
        _sessionStore = sessionStore;
        _messageSender = messageSender;
        _clock = clock;
    }

    public async Task<OperationResult<IssuedAccessChallenge>> RequestCodeAsync(
        AccessCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var recipient = Guard.AgainstNullOrWhiteSpace(request.Recipient, nameof(request.Recipient));
            var subjectId = Guard.AgainstNullOrWhiteSpace(request.SubjectId, nameof(request.SubjectId));
            var subjectName = Guard.AgainstNullOrWhiteSpace(request.SubjectDisplayName, nameof(request.SubjectDisplayName));
            var purpose = Guard.AgainstNullOrWhiteSpace(request.Purpose, nameof(request.Purpose));

            var code = SecureCodeGenerator.GenerateDigits(_options.CodeLength);
            var now = _clock.UtcNow;

            var challenge = new AccessChallenge
            {
                SubjectId = subjectId,
                SubjectDisplayName = subjectName,
                Recipient = recipient,
                Purpose = purpose,
                CodeHash = SecureCodeGenerator.ComputeSha256(code),
                CreatedAtUtc = now,
                ExpiresAtUtc = now.Add(_options.ChallengeTimeToLive),
                MaxAttempts = _options.MaxAttempts
            };

            await _challengeStore.SaveAsync(challenge, cancellationToken);

            await _messageSender.SendAsync(
                new AccessNotificationMessage(
                    recipient,
                    $"{_options.CodeSubjectPrefix} - {purpose}",
                    BuildBody(subjectName, purpose, code, challenge.ExpiresAtUtc),
                    code,
                    challenge.ExpiresAtUtc),
                cancellationToken);

            return OperationResult<IssuedAccessChallenge>.Success(
                new IssuedAccessChallenge(challenge.Id, MaskRecipient(recipient), challenge.ExpiresAtUtc));
        }
        catch (ArgumentException exception)
        {
            return OperationResult<IssuedAccessChallenge>.Failure(OperationError.Validation(exception.Message));
        }
    }

    public async Task<OperationResult<IssuedSession>> VerifyCodeAsync(
        VerifyAccessCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var challenge = await _challengeStore.FindByIdAsync(request.ChallengeId, cancellationToken);
        if (challenge is null)
        {
            return OperationResult<IssuedSession>.Failure(OperationError.NotFound("Desafio de acesso nao encontrado."));
        }

        var now = _clock.UtcNow;

        if (challenge.ConsumedAtUtc is not null)
        {
            return OperationResult<IssuedSession>.Failure(OperationError.Conflict("Este codigo ja foi utilizado."));
        }

        if (challenge.IsExpired(now))
        {
            return OperationResult<IssuedSession>.Failure(OperationError.Validation("O codigo expirou."));
        }

        if (!challenge.HasAttemptsRemaining)
        {
            return OperationResult<IssuedSession>.Failure(OperationError.Unauthorized("Numero maximo de tentativas atingido."));
        }

        var receivedCodeHash = SecureCodeGenerator.ComputeSha256(Guard.AgainstNullOrWhiteSpace(request.Code, nameof(request.Code)));
        if (!string.Equals(receivedCodeHash, challenge.CodeHash, StringComparison.Ordinal))
        {
            challenge.AttemptCount += 1;
            await _challengeStore.UpdateAsync(challenge, cancellationToken);
            return OperationResult<IssuedSession>.Failure(OperationError.Unauthorized("Codigo invalido."));
        }

        challenge.AttemptCount += 1;
        challenge.ConsumedAtUtc = now;
        await _challengeStore.UpdateAsync(challenge, cancellationToken);

        var rawToken = SecureCodeGenerator.GenerateToken();
        var session = new SessionTicket
        {
            SubjectId = challenge.SubjectId,
            SubjectDisplayName = challenge.SubjectDisplayName,
            Purpose = challenge.Purpose,
            TokenHash = SecureCodeGenerator.ComputeSha256(rawToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(_options.SessionTimeToLive)
        };

        await _sessionStore.SaveAsync(session, cancellationToken);

        return OperationResult<IssuedSession>.Success(
            new IssuedSession(session.Id, rawToken, session.SubjectId, session.SubjectDisplayName, session.Purpose, session.ExpiresAtUtc));
    }

    public async Task<OperationResult<SessionTicket>> ValidateSessionAsync(
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return OperationResult<SessionTicket>.Failure(OperationError.Validation("Token nao informado."));
        }

        var session = await _sessionStore.FindByTokenHashAsync(SecureCodeGenerator.ComputeSha256(rawToken.Trim()), cancellationToken);
        if (session is null)
        {
            return OperationResult<SessionTicket>.Failure(OperationError.NotFound("Sessao nao encontrada."));
        }

        if (!session.IsValid(_clock.UtcNow))
        {
            return OperationResult<SessionTicket>.Failure(OperationError.Unauthorized("Sessao invalida ou expirada."));
        }

        return OperationResult<SessionTicket>.Success(session);
    }

    public async Task<OperationResult> RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionStore.FindByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return OperationResult.Failure(OperationError.NotFound("Sessao nao encontrada."));
        }

        session.RevokedAtUtc = _clock.UtcNow;
        await _sessionStore.UpdateAsync(session, cancellationToken);

        return OperationResult.Success();
    }

    private static string BuildBody(string subjectName, string purpose, string code, DateTime expiresAtUtc)
    {
        return $"{subjectName}, use o codigo {code} para concluir {purpose}. Expira em {expiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC.";
    }

    private static string MaskRecipient(string recipient)
    {
        var atIndex = recipient.IndexOf('@');
        if (atIndex <= 1)
        {
            return "***";
        }

        return $"{recipient[0]}***{recipient[(atIndex - 1)]}{recipient[atIndex..]}";
    }
}
