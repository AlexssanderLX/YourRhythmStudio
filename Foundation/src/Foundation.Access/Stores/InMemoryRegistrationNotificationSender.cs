using Foundation.Access.Abstractions;
using Foundation.Access.Registrations;

namespace Foundation.Access.Stores;

public sealed class InMemoryRegistrationNotificationSender : IRegistrationNotificationSender
{
    private readonly List<RegistrationReviewRequestedMessage> _reviewRequests = [];
    private readonly List<RegistrationDecisionMessage> _decisions = [];

    public IReadOnlyCollection<RegistrationReviewRequestedMessage> SentReviewRequests => _reviewRequests.AsReadOnly();

    public IReadOnlyCollection<RegistrationDecisionMessage> SentDecisions => _decisions.AsReadOnly();

    public Task SendReviewRequestedAsync(RegistrationReviewRequestedMessage message, CancellationToken cancellationToken = default)
    {
        _reviewRequests.Add(message);
        return Task.CompletedTask;
    }

    public Task SendDecisionAsync(RegistrationDecisionMessage message, CancellationToken cancellationToken = default)
    {
        _decisions.Add(message);
        return Task.CompletedTask;
    }
}
