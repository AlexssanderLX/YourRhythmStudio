using System.Collections.Concurrent;
using Foundation.Assistant.Abstractions;
using Foundation.Assistant.Models;

namespace Foundation.Assistant.Conversations;

public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<string, List<ConversationTurn>> _storage = new(StringComparer.OrdinalIgnoreCase);

    public Task AppendAsync(string conversationId, ConversationTurn turn, int retainCount, CancellationToken cancellationToken = default)
    {
        var bucket = _storage.GetOrAdd(conversationId, _ => []);
        lock (bucket)
        {
            bucket.Add(turn);
            if (bucket.Count > retainCount)
            {
                bucket.RemoveRange(0, bucket.Count - retainCount);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<ConversationTurn>> GetRecentAsync(string conversationId, int take, CancellationToken cancellationToken = default)
    {
        if (!_storage.TryGetValue(conversationId, out var bucket))
        {
            return Task.FromResult<IReadOnlyCollection<ConversationTurn>>([]);
        }

        lock (bucket)
        {
            return Task.FromResult<IReadOnlyCollection<ConversationTurn>>(bucket.TakeLast(take).ToArray());
        }
    }
}
