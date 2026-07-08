using Foundation.Access.Abstractions;
using Foundation.Access.Authorization;
using Foundation.Access.Options;
using Foundation.Access.Security;
using Foundation.Access.Services;
using Foundation.Access.Stores;
using Foundation.Assistant.Abstractions;
using Foundation.Assistant.Ai;
using Foundation.Assistant.Conversations;
using Foundation.Assistant.Messaging;
using Foundation.Assistant.Options;
using Foundation.Assistant.Services;
using Foundation.Assistant.Templates;
using Foundation.Core.Abstractions;
using Foundation.Core.Utilities;
using Foundation.SecureLinks.Abstractions;
using Foundation.SecureLinks.Qr;
using Foundation.SecureLinks.Services;
using Foundation.SecureLinks.Stores;
using YourRhythmStudio.Domain;

namespace YourRhythmStudio.Infrastructure.Foundation;

public static class FoundationServiceCollectionExtensions
{
    public static IServiceCollection AddYourRhythmFoundation(this IServiceCollection services)
    {
        services.AddFoundationCore();
        services.AddFoundationAccess();
        services.AddFoundationSecureLinks();
        services.AddFoundationAssistant();

        return services;
    }

    private static IServiceCollection AddFoundationCore(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        return services;
    }

    private static IServiceCollection AddFoundationAccess(this IServiceCollection services)
    {
        services.AddSingleton(new AccessModuleOptions
        {
            CodeSubjectPrefix = "Seu codigo YourRhythm",
            DefaultTrialDays = 14,
            RequireAdministratorApprovalForRegistration = true,
            RequireActiveSubscriptionForTenantAccess = false,
            AdminReviewRecipients = []
        });

        services.AddSingleton<IAccountStore, MySqlBackedAccountStore>();
        services.AddSingleton<ITenantStore, InMemoryTenantStore>();
        services.AddSingleton<ITenantMembershipStore, InMemoryTenantMembershipStore>();
        services.AddSingleton<ISubscriptionPlanStore, InMemorySubscriptionPlanStore>();
        services.AddSingleton<ITenantSubscriptionStore, InMemoryTenantSubscriptionStore>();
        services.AddSingleton<IRegistrationRequestStore, InMemoryRegistrationRequestStore>();
        services.AddSingleton<IRegistrationNotificationSender, InMemoryRegistrationNotificationSender>();
        services.AddSingleton<ISessionTicketStore, InMemorySessionTicketStore>();
        services.AddSingleton<IAccessChallengeStore, InMemoryAccessChallengeStore>();
        services.AddSingleton<IAccessMessageSender, InMemoryAccessMessageSender>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        services.AddScoped<AccessService>();
        services.AddScoped<SaasAccessService>();
        services.AddScoped<AccessAuthorizationService>();
        services.AddScoped<YourRhythmAccessProfile>();

        return services;
    }

    private static IServiceCollection AddFoundationSecureLinks(this IServiceCollection services)
    {
        services.AddSingleton<ISecureLinkStore, InMemorySecureLinkStore>();
        services.AddSingleton<IQrArtifactGenerator, QRCoderSvgGenerator>();
        services.AddScoped<SecureLinkService>();

        return services;
    }

    private static IServiceCollection AddFoundationAssistant(this IServiceCollection services)
    {
        services.AddSingleton(new AssistantModuleOptions());
        services.AddSingleton<IConversationStore, InMemoryConversationStore>();
        services.AddSingleton<IMessageChannelClient, InMemoryMessageChannelClient>();
        services.AddSingleton<IAssistantCompletionProvider, RuleBasedAssistantCompletionProvider>();
        services.AddSingleton<AssistantTemplateService>();
        services.AddScoped<AssistantConversationService>();

        return services;
    }
}

public sealed class YourRhythmAccessProfile
{
    public string ProductName => "YourRhythm Studio";

    public IReadOnlyCollection<string> MvpFeatures => YourRhythmFeatures.Mvp;

    public IReadOnlyCollection<string> PlannedFeatures => YourRhythmFeatures.Planned;
}
