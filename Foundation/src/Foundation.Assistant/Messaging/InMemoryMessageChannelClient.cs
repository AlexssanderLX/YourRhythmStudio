using Foundation.Assistant.Abstractions;
using Foundation.Assistant.Models;

namespace Foundation.Assistant.Messaging;

public sealed class InMemoryMessageChannelClient : IMessageChannelClient
{
    private readonly List<OutboundConversationMessage> _messages = [];

    public IReadOnlyCollection<OutboundConversationMessage> SentMessages => _messages.AsReadOnly();

    public Task SendAsync(OutboundConversationMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Add(message);
        return Task.CompletedTask;
    }
}
