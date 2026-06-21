using Foundation.Assistant.Models;

namespace Foundation.Assistant.Abstractions;

public interface IConversationStore
{
    Task AppendAsync(string conversationId, ConversationTurn turn, int retainCount, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ConversationTurn>> GetRecentAsync(string conversationId, int take, CancellationToken cancellationToken = default);
}
