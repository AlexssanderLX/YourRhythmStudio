using System.Text;
using Foundation.Access.Abstractions;
using Foundation.Access.Accounts;
using Foundation.Access.Authentication;
using Foundation.Access.Options;
using Foundation.Access.Plans;
using Foundation.Access.Registrations;
using Foundation.Access.Security;
using Foundation.Access.Tenancy;
using Foundation.Access.Models;
using Foundation.Core.Abstractions;
using Foundation.Core.Models;
using Foundation.Core.Utilities;

namespace Foundation.Access.Services;

public sealed class SaasAccessService
{
    private readonly AccessModuleOptions _options;
    private readonly IAccountStore _accountStore;
    private readonly ITenantStore _tenantStore;
    private readonly ITenantMembershipStore _membershipStore;
    private readonly ISubscriptionPlanStore _planStore;
    private readonly ITenantSubscriptionStore _subscriptionStore;
    private readonly IRegistrationRequestStore _registrationStore;
    private readonly IRegistrationNotificationSender _registrationNotificationSender;
    private readonly ISessionTicketStore _sessionStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClock _clock;

    public SaasAccessService(
        AccessModuleOptions options,
        IAccountStore accountStore,
        ITenantStore tenantStore,
        ITenantMembershipStore membershipStore,
        ISubscriptionPlanStore planStore,
        ITenantSubscriptionStore subscriptionStore,
        IRegistrationRequestStore registrationStore,
        IRegistrationNotificationSender registrationNotificationSender,
        ISessionTicketStore sessionStore,
        IPasswordHasher passwordHasher,
        IClock clock)
    {
        _options = options;
        _accountStore = accountStore;
        _tenantStore = tenantStore;
        _membershipStore = membershipStore;
        _planStore = planStore;
        _subscriptionStore = subscriptionStore;
        _registrationStore = registrationStore;
        _registrationNotificationSender = registrationNotificationSender;
        _sessionStore = sessionStore;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task<OperationResult<Account>> CreatePlatformAdministratorAsync(
        CreatePlatformAdministratorRequest request,
        Guid? requestedByAccountId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var displayName = Guard.AgainstNullOrWhiteSpace(request.DisplayName, nameof(request.DisplayName));
            var email = NormalizeEmail(request.Email);
            var now = _clock.UtcNow;

            if (await _accountStore.AnyPlatformAdministratorAsync(cancellationToken))
            {
                var permission = await EnsurePlatformAdministratorAsync(requestedByAccountId, cancellationToken);
                if (permission.IsFailure || permission.Value is null)
                {
                    return OperationResult<Account>.Failure(permission.Error!);
                }
            }

            if (await _accountStore.FindByEmailAsync(email, cancellationToken) is not null)
            {
                return OperationResult<Account>.Failure(OperationError.Conflict("Ja existe uma conta com este e-mail."));
            }

            var account = new Account
            {
                DisplayName = displayName,
                Email = email,
                Status = AccountStatus.Active,
                PlatformRole = PlatformAccessRole.PlatformAdmin,
                PasswordCredential = _passwordHasher.HashPassword(request.Password),
                CreatedAtUtc = now,
                ActivatedAtUtc = now,
                SecurityStamp = SecureCodeGenerator.GenerateToken(16)
            };

            await _accountStore.SaveAsync(account, cancellationToken);
            return OperationResult<Account>.Success(account);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<Account>.Failure(OperationError.Validation(exception.Message));
        }
    }

    public async Task<OperationResult<SubscriptionPlan>> CreateSubscriptionPlanAsync(
        CreateSubscriptionPlanRequest request,
        Guid requestedByAccountId,
        CancellationToken cancellationToken = default)
    {
        var adminPermission = await EnsurePlatformAdministratorAsync(requestedByAccountId, cancellationToken);
        if (adminPermission.IsFailure)
        {
            return OperationResult<SubscriptionPlan>.Failure(adminPermission.Error!);
        }

        try
        {
            var code = NormalizeCode(request.Code);
            var displayName = Guard.AgainstNullOrWhiteSpace(request.DisplayName, nameof(request.DisplayName));
            var description = request.Description?.Trim() ?? string.Empty;
            Guard.AgainstNegative(request.MonthlyPrice, nameof(request.MonthlyPrice));
            Guard.AgainstNegative(request.SetupFee, nameof(request.SetupFee));
            if (request.YearlyPrice is not null)
            {
                Guard.AgainstNegative(request.YearlyPrice.Value, nameof(request.YearlyPrice));
            }

            if (await _planStore.FindByCodeAsync(code, cancellationToken) is not null)
            {
                return OperationResult<SubscriptionPlan>.Failure(OperationError.Conflict("Ja existe um plano com este codigo."));
            }

            var plan = new SubscriptionPlan
            {
                Code = code,
                DisplayName = displayName,
                Description = description,
                MonthlyPrice = request.MonthlyPrice,
                YearlyPrice = request.YearlyPrice,
                SetupFee = request.SetupFee,
                IncludedFeatures = NormalizeFeatures(request.IncludedFeatures),
                CreatedAtUtc = _clock.UtcNow
            };

            await _planStore.SaveAsync(plan, cancellationToken);
            return OperationResult<SubscriptionPlan>.Success(plan);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<SubscriptionPlan>.Failure(OperationError.Validation(exception.Message));
        }
    }

    public async Task<OperationResult<IReadOnlyCollection<TenantRegistrationRequest>>> ListPendingRegistrationsAsync(
        Guid requestedByAccountId,
        CancellationToken cancellationToken = default)
    {
        var adminPermission = await EnsurePlatformAdministratorAsync(requestedByAccountId, cancellationToken);
        if (adminPermission.IsFailure)
        {
            return OperationResult<IReadOnlyCollection<TenantRegistrationRequest>>.Failure(adminPermission.Error!);
        }

        var pending = await _registrationStore.ListPendingAsync(cancellationToken);
        return OperationResult<IReadOnlyCollection<TenantRegistrationRequest>>.Success(pending);
    }

    public async Task<OperationResult<IReadOnlyCollection<SubscriptionPlan>>> ListActivePlansAsync(
        CancellationToken cancellationToken = default)
    {
        var plans = await _planStore.ListActiveAsync(cancellationToken);
        return OperationResult<IReadOnlyCollection<SubscriptionPlan>>.Success(plans);
    }

    public async Task<OperationResult<RegistrationReceipt>> RegisterTenantAsync(
        RegisterTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_options.AllowSelfServiceRegistration)
        {
            return OperationResult<RegistrationReceipt>.Failure(OperationError.Unauthorized("O cadastro automatico de tenants nao esta habilitado."));
        }

        try
        {
            var tenantDisplayName = Guard.AgainstNullOrWhiteSpace(request.TenantDisplayName, nameof(request.TenantDisplayName));
            var tenantKey = NormalizeKey(request.TenantKey, tenantDisplayName);
            var ownerDisplayName = Guard.AgainstNullOrWhiteSpace(request.OwnerDisplayName, nameof(request.OwnerDisplayName));
            var ownerEmail = NormalizeEmail(request.OwnerEmail);

            if (await _tenantStore.FindByKeyAsync(tenantKey, cancellationToken) is not null)
            {
                return OperationResult<RegistrationReceipt>.Failure(OperationError.Conflict("Ja existe um tenant com esta chave."));
            }

            if (await _accountStore.FindByEmailAsync(ownerEmail, cancellationToken) is not null)
            {
                return OperationResult<RegistrationReceipt>.Failure(OperationError.Conflict("Ja existe uma conta com este e-mail."));
            }

            if (await _registrationStore.FindPendingByEmailAsync(ownerEmail, cancellationToken) is not null)
            {
                return OperationResult<RegistrationReceipt>.Failure(OperationError.Conflict("Ja existe uma solicitacao pendente para este e-mail."));
            }

            if (!string.IsNullOrWhiteSpace(request.RequestedPlanCode))
            {
                var requestedPlan = await _planStore.FindByCodeAsync(request.RequestedPlanCode, cancellationToken);
                if (requestedPlan is null || !requestedPlan.IsActive)
                {
                    return OperationResult<RegistrationReceipt>.Failure(OperationError.Validation("O plano informado nao existe ou esta inativo."));
                }
            }

            var registration = new TenantRegistrationRequest
            {
                TenantDisplayName = tenantDisplayName,
                TenantKey = tenantKey,
                OwnerDisplayName = ownerDisplayName,
                OwnerEmail = ownerEmail,
                PasswordCredential = _passwordHasher.HashPassword(request.Password),
                RequestedPlanCode = NormalizeOptionalCode(request.RequestedPlanCode),
                CreatedAtUtc = _clock.UtcNow
            };

            await _registrationStore.SaveAsync(registration, cancellationToken);

            if (_options.RequireAdministratorApprovalForRegistration)
            {
                if (_options.AdminReviewRecipients.Count > 0)
                {
                    await _registrationNotificationSender.SendReviewRequestedAsync(
                        new RegistrationReviewRequestedMessage(
                            _options.AdminReviewRecipients,
                            registration.Id,
                            registration.TenantDisplayName,
                            registration.TenantKey,
                            registration.OwnerDisplayName,
                            registration.OwnerEmail,
                            registration.RequestedPlanCode,
                            registration.CreatedAtUtc),
                        cancellationToken);
                }

                return OperationResult<RegistrationReceipt>.Success(
                    new RegistrationReceipt(registration.Id, registration.OwnerEmail, true, registration.CreatedAtUtc));
            }

            var autoApproval = await ApproveRegistrationInternalAsync(registration, null, "Aprovacao automatica.", cancellationToken);
            if (autoApproval.IsFailure)
            {
                return OperationResult<RegistrationReceipt>.Failure(autoApproval.Error!);
            }

            return OperationResult<RegistrationReceipt>.Success(
                new RegistrationReceipt(registration.Id, registration.OwnerEmail, false, registration.CreatedAtUtc));
        }
        catch (ArgumentException exception)
        {
            return OperationResult<RegistrationReceipt>.Failure(OperationError.Validation(exception.Message));
        }
    }

    public async Task<OperationResult<ReviewRegistrationResult>> ReviewRegistrationAsync(
        ReviewRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminPermission = await EnsurePlatformAdministratorAsync(request.ReviewedByAccountId, cancellationToken);
        if (adminPermission.IsFailure)
        {
            return OperationResult<ReviewRegistrationResult>.Failure(adminPermission.Error!);
        }

        var registration = await _registrationStore.FindByIdAsync(request.RegistrationRequestId, cancellationToken);
        if (registration is null)
        {
            return OperationResult<ReviewRegistrationResult>.Failure(OperationError.NotFound("Solicitacao de cadastro nao encontrada."));
        }

        if (registration.Status != RegistrationRequestStatus.Pending)
        {
            return OperationResult<ReviewRegistrationResult>.Failure(OperationError.Conflict("Esta solicitacao ja foi analisada."));
        }

        if (request.Approve)
        {
            return await ApproveRegistrationInternalAsync(registration, request.ReviewedByAccountId, request.Notes, cancellationToken);
        }

        registration.Status = RegistrationRequestStatus.Rejected;
        registration.ReviewedByAccountId = request.ReviewedByAccountId;
        registration.ReviewedAtUtc = _clock.UtcNow;
        registration.ReviewNotes = request.Notes?.Trim();
        await _registrationStore.UpdateAsync(registration, cancellationToken);

        await _registrationNotificationSender.SendDecisionAsync(
            new RegistrationDecisionMessage(
                registration.OwnerEmail,
                false,
                registration.TenantDisplayName,
                registration.OwnerDisplayName,
                string.IsNullOrWhiteSpace(request.Notes)
                    ? "Seu cadastro foi analisado e nao foi aprovado neste momento."
                    : request.Notes!,
                registration.ReviewedAtUtc.Value),
            cancellationToken);

        return OperationResult<ReviewRegistrationResult>.Success(
            new ReviewRegistrationResult(registration.Id, registration.Status, null, null, null));
    }

    public async Task<OperationResult<TenantSubscription>> AssignPlanAsync(
        AssignSubscriptionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminPermission = await EnsurePlatformAdministratorAsync(request.UpdatedByAccountId, cancellationToken);
        if (adminPermission.IsFailure)
        {
            return OperationResult<TenantSubscription>.Failure(adminPermission.Error!);
        }

        var tenant = await _tenantStore.FindByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return OperationResult<TenantSubscription>.Failure(OperationError.NotFound("Tenant nao encontrado."));
        }

        var plan = await _planStore.FindByIdAsync(request.PlanId, cancellationToken);
        if (plan is null || !plan.IsActive)
        {
            return OperationResult<TenantSubscription>.Failure(OperationError.NotFound("Plano nao encontrado ou inativo."));
        }

        var now = _clock.UtcNow;
        var current = await _subscriptionStore.FindCurrentByTenantIdAsync(tenant.Id, cancellationToken);
        var trialDays = request.OverrideTrialDays ?? _options.DefaultTrialDays;

        if (current is null)
        {
            current = new TenantSubscription
            {
                TenantId = tenant.Id,
                PlanId = plan.Id,
                BillingEmail = string.IsNullOrWhiteSpace(request.BillingEmail) ? tenant.PrimaryEmail : NormalizeEmail(request.BillingEmail),
                StartsAtUtc = now,
                Status = request.StartTrial && trialDays > 0 ? SubscriptionStatus.Trial : SubscriptionStatus.Active,
                TrialEndsAtUtc = request.StartTrial && trialDays > 0 ? now.AddDays(trialDays) : null
            };

            await _subscriptionStore.SaveAsync(current, cancellationToken);
        }
        else
        {
            current.PlanId = plan.Id;
            current.BillingEmail = string.IsNullOrWhiteSpace(request.BillingEmail) ? current.BillingEmail : NormalizeEmail(request.BillingEmail);
            current.StartsAtUtc = now;
            current.Status = request.StartTrial && trialDays > 0 ? SubscriptionStatus.Trial : SubscriptionStatus.Active;
            current.TrialEndsAtUtc = request.StartTrial && trialDays > 0 ? now.AddDays(trialDays) : null;
            current.EndsAtUtc = null;

            await _subscriptionStore.UpdateAsync(current, cancellationToken);
        }

        return OperationResult<TenantSubscription>.Success(current);
    }

    public async Task<OperationResult<IssuedSaasSession>> SignInWithPasswordAsync(
        PasswordSignInRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var email = NormalizeEmail(request.Email);
            var password = Guard.AgainstNullOrWhiteSpace(request.Password, nameof(request.Password));

            var account = await _accountStore.FindByEmailAsync(email, cancellationToken);
            if (account is null)
            {
                return OperationResult<IssuedSaasSession>.Failure(OperationError.NotFound("Conta nao encontrada."));
            }

            if (account.Status != AccountStatus.Active)
            {
                return OperationResult<IssuedSaasSession>.Failure(OperationError.Unauthorized("A conta nao esta ativa."));
            }

            if (account.PasswordCredential is null)
            {
                return OperationResult<IssuedSaasSession>.Failure(OperationError.Unauthorized("A conta nao possui senha configurada."));
            }

            var passwordVerification = _passwordHasher.Verify(password, account.PasswordCredential);
            if (!passwordVerification.IsSuccess)
            {
                return OperationResult<IssuedSaasSession>.Failure(OperationError.Unauthorized("Senha invalida."));
            }

            if (passwordVerification.NeedsRehash)
            {
                account.PasswordCredential = _passwordHasher.HashPassword(password);
                account.SecurityStamp = SecureCodeGenerator.GenerateToken(16);
                await _accountStore.UpdateAsync(account, cancellationToken);
            }

            Tenant? tenant = null;
            TenantMembership? membership = null;
            SubscriptionPlan? plan = null;
            TenantSubscription? subscription = null;
            IReadOnlyCollection<string> enabledFeatures = Array.Empty<string>();

            var tenantResolution = await ResolveTenantContextAsync(account, request.TenantKey, cancellationToken);
            if (tenantResolution.IsFailure)
            {
                return OperationResult<IssuedSaasSession>.Failure(tenantResolution.Error!);
            }

            if (tenantResolution.Value is not null)
            {
                var resolved = tenantResolution.Value.Value;
                tenant = resolved.Tenant;
                membership = resolved.Membership;
                plan = resolved.Plan;
                subscription = resolved.Subscription;
                enabledFeatures = resolved.EnabledFeatures;
            }

            var session = await IssueSessionAsync(account, tenant, membership, plan, cancellationToken);
            return OperationResult<IssuedSaasSession>.Success(
                new IssuedSaasSession(
                    session.Record.Id,
                    session.TokenRaw,
                    account.Id,
                    account.DisplayName,
                    account.Email,
                    account.PlatformRole,
                    tenant?.Id,
                    tenant?.DisplayName,
                    membership?.Role,
                    plan?.Code,
                    enabledFeatures,
                    session.Record.ExpiresAtUtc));
        }
        catch (ArgumentException exception)
        {
            return OperationResult<IssuedSaasSession>.Failure(OperationError.Validation(exception.Message));
        }
    }

    public async Task<OperationResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = await _accountStore.FindByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            return OperationResult.Failure(OperationError.NotFound("Conta nao encontrada."));
        }

        if (account.Status != AccountStatus.Active || account.PasswordCredential is null)
        {
            return OperationResult.Failure(OperationError.Unauthorized("A conta nao esta disponivel para troca de senha."));
        }

        try
        {
            var currentPassword = Guard.AgainstNullOrWhiteSpace(request.CurrentPassword, nameof(request.CurrentPassword));
            var newPassword = Guard.AgainstNullOrWhiteSpace(request.NewPassword, nameof(request.NewPassword));

            var verification = _passwordHasher.Verify(currentPassword, account.PasswordCredential);
            if (!verification.IsSuccess)
            {
                return OperationResult.Failure(OperationError.Unauthorized("Senha atual invalida."));
            }

            account.PasswordCredential = _passwordHasher.HashPassword(newPassword);
            account.SecurityStamp = SecureCodeGenerator.GenerateToken(16);
            await _accountStore.UpdateAsync(account, cancellationToken);

            if (request.RevokeSessions)
            {
                await RevokeSessionsForAccountAsync(account.Id, cancellationToken);
            }

            return OperationResult.Success();
        }
        catch (ArgumentException exception)
        {
            return OperationResult.Failure(OperationError.Validation(exception.Message));
        }
    }

    public async Task<OperationResult> AdminResetPasswordAsync(
        AdminResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminPermission = await EnsurePlatformAdministratorAsync(request.RequestedByAccountId, cancellationToken);
        if (adminPermission.IsFailure)
        {
            return OperationResult.Failure(adminPermission.Error!);
        }

        var account = await _accountStore.FindByIdAsync(request.TargetAccountId, cancellationToken);
        if (account is null)
        {
            return OperationResult.Failure(OperationError.NotFound("Conta alvo nao encontrada."));
        }

        try
        {
            var newPassword = Guard.AgainstNullOrWhiteSpace(request.NewPassword, nameof(request.NewPassword));
            account.PasswordCredential = _passwordHasher.HashPassword(newPassword);
            account.SecurityStamp = SecureCodeGenerator.GenerateToken(16);
            await _accountStore.UpdateAsync(account, cancellationToken);

            if (request.RevokeSessions)
            {
                await RevokeSessionsForAccountAsync(account.Id, cancellationToken);
            }

            return OperationResult.Success();
        }
        catch (ArgumentException exception)
        {
            return OperationResult.Failure(OperationError.Validation(exception.Message));
        }
    }

    public async Task<OperationResult<SaasAccessContext>> ValidateSaasSessionAsync(
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return OperationResult<SaasAccessContext>.Failure(OperationError.Validation("Token nao informado."));
        }

        var tokenHash = SecureCodeGenerator.ComputeSha256(rawToken.Trim());
        var session = await _sessionStore.FindByTokenHashAsync(tokenHash, cancellationToken);
        if (session is null || !session.IsValid(_clock.UtcNow))
        {
            return OperationResult<SaasAccessContext>.Failure(OperationError.Unauthorized("Sessao invalida ou expirada."));
        }

        if (session.AccountId is null)
        {
            return OperationResult<SaasAccessContext>.Failure(OperationError.Validation("A sessao atual nao possui contexto de conta SaaS."));
        }

        var account = await _accountStore.FindByIdAsync(session.AccountId.Value, cancellationToken);
        if (account is null || account.Status != AccountStatus.Active)
        {
            return OperationResult<SaasAccessContext>.Failure(OperationError.Unauthorized("A conta associada a sessao nao esta disponivel."));
        }

        Tenant? tenant = null;
        TenantMembership? membership = null;
        SubscriptionPlan? plan = null;
        TenantSubscription? subscription = null;
        IReadOnlyCollection<string> enabledFeatures = Array.Empty<string>();

        if (session.TenantId is not null)
        {
            tenant = await _tenantStore.FindByIdAsync(session.TenantId.Value, cancellationToken);
            if (tenant is null || !tenant.IsActive)
            {
                return OperationResult<SaasAccessContext>.Failure(OperationError.Unauthorized("O tenant associado a sessao nao esta ativo."));
            }

            membership = await _membershipStore.FindByAccountAndTenantAsync(account.Id, tenant.Id, cancellationToken);
            if (membership is null || !membership.IsActive)
            {
                return OperationResult<SaasAccessContext>.Failure(OperationError.Unauthorized("A conta nao possui mais acesso ativo ao tenant."));
            }

            subscription = await _subscriptionStore.FindCurrentByTenantIdAsync(tenant.Id, cancellationToken);
            if (_options.RequireActiveSubscriptionForTenantAccess && (subscription is null || !subscription.IsActive(_clock.UtcNow)))
            {
                return OperationResult<SaasAccessContext>.Failure(OperationError.Unauthorized("O tenant nao possui assinatura ativa para acesso."));
            }

            if (subscription is not null)
            {
                plan = await _planStore.FindByIdAsync(subscription.PlanId, cancellationToken);
                enabledFeatures = NormalizeFeatures(plan?.IncludedFeatures);
            }
        }

        return OperationResult<SaasAccessContext>.Success(
            new SaasAccessContext(session, account, tenant, membership, plan, subscription, enabledFeatures));
    }

    private async Task<OperationResult<ReviewRegistrationResult>> ApproveRegistrationInternalAsync(
        TenantRegistrationRequest registration,
        Guid? reviewedByAccountId,
        string? notes,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        var account = new Account
        {
            DisplayName = registration.OwnerDisplayName,
            Email = registration.OwnerEmail,
            Status = AccountStatus.Active,
            PlatformRole = PlatformAccessRole.None,
            PasswordCredential = registration.PasswordCredential,
            CreatedAtUtc = now,
            ActivatedAtUtc = now,
            SecurityStamp = SecureCodeGenerator.GenerateToken(16)
        };

        var tenant = new Tenant
        {
            Key = registration.TenantKey,
            DisplayName = registration.TenantDisplayName,
            PrimaryEmail = registration.OwnerEmail,
            Status = TenantStatus.Active,
            OwnerAccountId = account.Id,
            CreatedAtUtc = now,
            ApprovedAtUtc = now
        };

        var membership = new TenantMembership
        {
            TenantId = tenant.Id,
            AccountId = account.Id,
            Role = TenantAccessRole.Owner,
            IsPrimary = true,
            JoinedAtUtc = now
        };

        await _accountStore.SaveAsync(account, cancellationToken);
        await _tenantStore.SaveAsync(tenant, cancellationToken);
        await _membershipStore.SaveAsync(membership, cancellationToken);

        TenantSubscription? subscription = null;
        if (!string.IsNullOrWhiteSpace(registration.RequestedPlanCode))
        {
            var plan = await _planStore.FindByCodeAsync(registration.RequestedPlanCode, cancellationToken);
            if (plan is not null && plan.IsActive)
            {
                var isTrial = _options.DefaultTrialDays > 0;
                subscription = new TenantSubscription
                {
                    TenantId = tenant.Id,
                    PlanId = plan.Id,
                    BillingEmail = tenant.PrimaryEmail,
                    StartsAtUtc = now,
                    Status = isTrial ? SubscriptionStatus.Trial : SubscriptionStatus.Active,
                    TrialEndsAtUtc = isTrial ? now.AddDays(_options.DefaultTrialDays) : null
                };

                await _subscriptionStore.SaveAsync(subscription, cancellationToken);
            }
        }

        registration.Status = RegistrationRequestStatus.Approved;
        registration.ReviewedByAccountId = reviewedByAccountId;
        registration.ReviewedAtUtc = now;
        registration.ReviewNotes = notes?.Trim();
        await _registrationStore.UpdateAsync(registration, cancellationToken);

        await _registrationNotificationSender.SendDecisionAsync(
            new RegistrationDecisionMessage(
                registration.OwnerEmail,
                true,
                registration.TenantDisplayName,
                registration.OwnerDisplayName,
                "Seu cadastro foi aprovado e o ambiente ja pode ser acessado.",
                now),
            cancellationToken);

        return OperationResult<ReviewRegistrationResult>.Success(
            new ReviewRegistrationResult(registration.Id, registration.Status, account.Id, tenant.Id, subscription?.Id));
    }

    private async Task<OperationResult<Account>> EnsurePlatformAdministratorAsync(Guid? requestedByAccountId, CancellationToken cancellationToken)
    {
        if (requestedByAccountId is null)
        {
            return OperationResult<Account>.Failure(OperationError.Unauthorized("A operacao exige um administrador da plataforma."));
        }

        var account = await _accountStore.FindByIdAsync(requestedByAccountId.Value, cancellationToken);
        if (account is null || account.PlatformRole != PlatformAccessRole.PlatformAdmin || account.Status != AccountStatus.Active)
        {
            return OperationResult<Account>.Failure(OperationError.Unauthorized("A conta informada nao possui acesso de administrador da plataforma."));
        }

        return OperationResult<Account>.Success(account);
    }

    private async Task<OperationResult<(Tenant Tenant, TenantMembership Membership, SubscriptionPlan? Plan, TenantSubscription? Subscription, IReadOnlyCollection<string> EnabledFeatures)?>>
        ResolveTenantContextAsync(
            Account account,
            string? requestedTenantKey,
            CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestedTenantKey))
        {
            var memberships = (await _membershipStore.ListByAccountAsync(account.Id, cancellationToken))
                .Where(item => item.IsActive)
                .ToArray();

            if (memberships.Length == 0)
            {
                return OperationResult<(Tenant, TenantMembership, SubscriptionPlan?, TenantSubscription?, IReadOnlyCollection<string>)?>.Success(null);
            }

            if (memberships.Length > 1)
            {
                return OperationResult<(Tenant, TenantMembership, SubscriptionPlan?, TenantSubscription?, IReadOnlyCollection<string>)?>.Failure(
                    OperationError.Validation("Esta conta possui mais de um tenant. Informe a chave do tenant para concluir o acesso."));
            }

            var tenant = await _tenantStore.FindByIdAsync(memberships[0].TenantId, cancellationToken);
            if (tenant is null || !tenant.IsActive)
            {
                return OperationResult<(Tenant, TenantMembership, SubscriptionPlan?, TenantSubscription?, IReadOnlyCollection<string>)?>.Failure(
                    OperationError.Unauthorized("O tenant associado a conta nao esta ativo."));
            }

            var subscription = await _subscriptionStore.FindCurrentByTenantIdAsync(tenant.Id, cancellationToken);
            if (_options.RequireActiveSubscriptionForTenantAccess && (subscription is null || !subscription.IsActive(_clock.UtcNow)))
            {
                return OperationResult<(Tenant, TenantMembership, SubscriptionPlan?, TenantSubscription?, IReadOnlyCollection<string>)?>.Failure(
                    OperationError.Unauthorized("O tenant nao possui assinatura ativa para acesso."));
            }

            SubscriptionPlan? plan = null;
            IReadOnlyCollection<string> enabledFeatures = Array.Empty<string>();
            if (subscription is not null)
            {
                plan = await _planStore.FindByIdAsync(subscription.PlanId, cancellationToken);
                enabledFeatures = NormalizeFeatures(plan?.IncludedFeatures);
            }

            return OperationResult<(Tenant, TenantMembership, SubscriptionPlan?, TenantSubscription?, IReadOnlyCollection<string>)?>.Success(
                (tenant, memberships[0], plan, subscription, enabledFeatures));
        }

        var tenantKey = NormalizeKey(requestedTenantKey, requestedTenantKey);
        var requestedTenant = await _tenantStore.FindByKeyAsync(tenantKey, cancellationToken);
        if (requestedTenant is null || !requestedTenant.IsActive)
        {
            return OperationResult<(Tenant, TenantMembership, SubscriptionPlan?, TenantSubscription?, IReadOnlyCollection<string>)?>.Failure(
                OperationError.NotFound("Tenant nao encontrado."));
        }

        var membershipForTenant = await _membershipStore.FindByAccountAndTenantAsync(account.Id, requestedTenant.Id, cancellationToken);
        if (membershipForTenant is null || !membershipForTenant.IsActive)
        {
            return OperationResult<(Tenant, TenantMembership, SubscriptionPlan?, TenantSubscription?, IReadOnlyCollection<string>)?>.Failure(
                OperationError.Unauthorized("A conta nao possui acesso a este tenant."));
        }

        var requestedSubscription = await _subscriptionStore.FindCurrentByTenantIdAsync(requestedTenant.Id, cancellationToken);
        if (_options.RequireActiveSubscriptionForTenantAccess && (requestedSubscription is null || !requestedSubscription.IsActive(_clock.UtcNow)))
        {
            return OperationResult<(Tenant, TenantMembership, SubscriptionPlan?, TenantSubscription?, IReadOnlyCollection<string>)?>.Failure(
                OperationError.Unauthorized("O tenant nao possui assinatura ativa para acesso."));
        }

        SubscriptionPlan? requestedPlan = null;
        IReadOnlyCollection<string> requestedFeatures = Array.Empty<string>();
        if (requestedSubscription is not null)
        {
            requestedPlan = await _planStore.FindByIdAsync(requestedSubscription.PlanId, cancellationToken);
            requestedFeatures = NormalizeFeatures(requestedPlan?.IncludedFeatures);
        }

        return OperationResult<(Tenant, TenantMembership, SubscriptionPlan?, TenantSubscription?, IReadOnlyCollection<string>)?>.Success(
            (requestedTenant, membershipForTenant, requestedPlan, requestedSubscription, requestedFeatures));
    }

    private async Task<(SessionTicket Record, string TokenRaw)> IssueSessionAsync(
        Account account,
        Tenant? tenant,
        TenantMembership? membership,
        SubscriptionPlan? plan,
        CancellationToken cancellationToken)
    {
        var rawToken = SecureCodeGenerator.GenerateToken();
        var session = new SessionTicket
        {
            SubjectId = account.Id.ToString("N"),
            SubjectDisplayName = account.DisplayName,
            AccountId = account.Id,
            Email = account.Email,
            Purpose = tenant is null ? "password-sign-in" : $"password-sign-in:{tenant.Key}",
            TenantId = tenant?.Id,
            TenantDisplayName = tenant?.DisplayName,
            PlatformRole = account.PlatformRole,
            TenantRole = membership?.Role,
            PlanCode = plan?.Code,
            TokenHash = SecureCodeGenerator.ComputeSha256(rawToken),
            CreatedAtUtc = _clock.UtcNow,
            ExpiresAtUtc = _clock.UtcNow.Add(_options.SessionTimeToLive)
        };

        await _sessionStore.SaveAsync(session, cancellationToken);
        return (session, rawToken);
    }

    private async Task RevokeSessionsForAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var sessions = await _sessionStore.ListByAccountIdAsync(accountId, cancellationToken);
        foreach (var session in sessions.Where(item => item.RevokedAtUtc is null))
        {
            session.RevokedAtUtc = _clock.UtcNow;
            await _sessionStore.UpdateAsync(session, cancellationToken);
        }
    }

    private static string NormalizeEmail(string email) => Guard.AgainstNullOrWhiteSpace(email, nameof(email)).ToUpperInvariant();

    private static string NormalizeOptionalCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : NormalizeCode(value);
    }

    private static string NormalizeCode(string value) => Guard.AgainstNullOrWhiteSpace(value, nameof(value)).ToUpperInvariant();

    private static List<string> NormalizeFeatures(IEnumerable<string>? includedFeatures)
    {
        return includedFeatures?
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
    }

    private static string NormalizeKey(string? rawKey, string fallbackDisplayName)
    {
        if (!string.IsNullOrWhiteSpace(rawKey))
        {
            return Slugify(rawKey);
        }

        return Slugify(fallbackDisplayName);
    }

    private static string Slugify(string value)
    {
        var input = Guard.AgainstNullOrWhiteSpace(value, nameof(value)).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(input.Length);
        var previousHyphen = false;

        foreach (var character in input)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousHyphen = false;
                continue;
            }

            if (!previousHyphen)
            {
                builder.Append('-');
                previousHyphen = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Nao foi possivel gerar uma chave valida a partir do valor informado.", nameof(value));
        }

        return slug;
    }
}
