using Foundation.Access.Accounts;
using Foundation.Access.Models;
using Foundation.Access.Plans;
using Foundation.Access.Tenancy;

namespace Foundation.Access.Authentication;

public sealed record SaasAccessContext(
    SessionTicket Session,
    Account Account,
    Tenant? Tenant,
    TenantMembership? Membership,
    SubscriptionPlan? Plan,
    TenantSubscription? Subscription,
    IReadOnlyCollection<string> EnabledFeatures);
