using Foundation.Access.Registrations;

namespace Foundation.Access.Abstractions;

public interface IRegistrationNotificationSender
{
    Task SendReviewRequestedAsync(RegistrationReviewRequestedMessage message, CancellationToken cancellationToken = default);

    Task SendDecisionAsync(RegistrationDecisionMessage message, CancellationToken cancellationToken = default);
}
