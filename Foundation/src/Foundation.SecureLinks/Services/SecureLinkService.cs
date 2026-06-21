using Foundation.Core.Abstractions;
using Foundation.Core.Models;
using Foundation.Core.Utilities;
using Foundation.SecureLinks.Abstractions;
using Foundation.SecureLinks.Models;

namespace Foundation.SecureLinks.Services;

public sealed class SecureLinkService
{
    private readonly ISecureLinkStore _store;
    private readonly IClock _clock;

    public SecureLinkService(ISecureLinkStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    public async Task<OperationResult<IssuedSecureLink>> CreateAsync(
        Uri baseUri,
        CreateSecureLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var label = Guard.AgainstNullOrWhiteSpace(request.Label, nameof(request.Label));
            var resourceKey = Guard.AgainstNullOrWhiteSpace(request.ResourceKey, nameof(request.ResourceKey));
            var relativePath = Guard.AgainstNullOrWhiteSpace(request.RelativePath, nameof(request.RelativePath));

            var publicCode = SecureCodeGenerator.GenerateAlphaNumeric(24);
            var record = new SecureLinkRecord
            {
                Label = label,
                ResourceKey = resourceKey,
                RelativePath = relativePath.StartsWith('/') ? relativePath : $"/{relativePath}",
                PublicCode = publicCode,
                CreatedAtUtc = _clock.UtcNow,
                ExpiresAtUtc = request.ExpiresAtUtc,
                MaxUsages = request.MaxUsages
            };

            await _store.SaveAsync(record, cancellationToken);

            return OperationResult<IssuedSecureLink>.Success(
                new IssuedSecureLink(
                    record.Id,
                    record.PublicCode,
                    BuildAbsoluteUrl(baseUri, record.RelativePath),
                    record.ExpiresAtUtc,
                    record.MaxUsages));
        }
        catch (ArgumentException exception)
        {
            return OperationResult<IssuedSecureLink>.Failure(OperationError.Validation(exception.Message));
        }
    }

    public async Task<OperationResult<SecureLinkResolution>> ResolveAsync(
        Uri baseUri,
        string publicCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicCode))
        {
            return OperationResult<SecureLinkResolution>.Failure(OperationError.Validation("Codigo publico nao informado."));
        }

        var record = await _store.FindByPublicCodeAsync(publicCode.Trim(), cancellationToken);
        if (record is null)
        {
            return OperationResult<SecureLinkResolution>.Failure(OperationError.NotFound("Link seguro nao encontrado."));
        }

        var now = _clock.UtcNow;
        if (!record.IsActive)
        {
            return OperationResult<SecureLinkResolution>.Failure(OperationError.Conflict("Link inativo."));
        }

        if (record.IsExpired(now))
        {
            return OperationResult<SecureLinkResolution>.Failure(OperationError.Conflict("Link expirado."));
        }

        if (!record.HasUsageAvailable)
        {
            return OperationResult<SecureLinkResolution>.Failure(OperationError.Conflict("Link sem usos disponiveis."));
        }

        record.UsageCount += 1;
        await _store.UpdateAsync(record, cancellationToken);

        return OperationResult<SecureLinkResolution>.Success(
            new SecureLinkResolution(
                record.Id,
                record.PublicCode,
                record.ResourceKey,
                BuildAbsoluteUrl(baseUri, record.RelativePath),
                record.UsageCount,
                false,
                record.IsActive));
    }

    public async Task<OperationResult> DeactivateAsync(string publicCode, CancellationToken cancellationToken = default)
    {
        var record = await _store.FindByPublicCodeAsync(publicCode, cancellationToken);
        if (record is null)
        {
            return OperationResult.Failure(OperationError.NotFound("Link seguro nao encontrado."));
        }

        record.IsActive = false;
        await _store.UpdateAsync(record, cancellationToken);
        return OperationResult.Success();
    }

    private static string BuildAbsoluteUrl(Uri baseUri, string relativePath)
    {
        return new Uri(baseUri, relativePath).ToString();
    }
}
