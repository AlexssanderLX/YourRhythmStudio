using Foundation.Assistant.Models;

namespace Foundation.Assistant.Abstractions;

public interface IMessageChannelClient
{
    Task SendAsync(OutboundConversationMessage message, CancellationToken cancellationToken = default);
}
