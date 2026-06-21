using Foundation.Access.Accounts;
using Foundation.Access.Authentication;
using Foundation.Access.Tenancy;
using Foundation.Core.Models;

namespace Foundation.Access.Authorization;

public sealed class AccessAuthorizationService
{
    public OperationResult RequirePlatformAdministrator(Account account)
    {
        if (account.PlatformRole != PlatformAccessRole.PlatformAdmin || !account.IsActive)
        {
            return OperationResult.Failure(OperationError.Unauthorized("A conta atual nao possui acesso de administrador da plataforma."));
        }

        return OperationResult.Success();
    }

    public OperationResult RequireTenantRole(SaasAccessContext context, params TenantAccessRole[] allowedRoles)
    {
        if (context.Membership is null || !context.Membership.IsActive || context.Tenant is null || !context.Tenant.IsActive)
        {
            return OperationResult.Failure(OperationError.Unauthorized("Nao existe um contexto de tenant ativo para esta sessao."));
        }

        if (allowedRoles.Length > 0 && !allowedRoles.Contains(context.Membership.Role))
        {
            return OperationResult.Failure(OperationError.Unauthorized("A conta nao possui o papel necessario nesse tenant."));
        }

        return OperationResult.Success();
    }

    public bool HasFeature(SaasAccessContext context, string featureCode)
    {
        if (string.IsNullOrWhiteSpace(featureCode))
        {
            return false;
        }

        return context.EnabledFeatures.Contains(featureCode.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
