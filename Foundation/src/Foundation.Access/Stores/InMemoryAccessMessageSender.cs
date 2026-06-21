using Foundation.Access.Abstractions;
using Foundation.Access.Models;

namespace Foundation.Access.Stores;

public sealed class InMemoryAccessMessageSender : IAccessMessageSender
{
    private readonly List<AccessNotificationMessage> _messages = [];

    public IReadOnlyCollection<AccessNotificationMessage> SentMessages => _messages.AsReadOnly();

    public Task SendAsync(AccessNotificationMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Add(message);
        return Task.CompletedTask;
    }
}
