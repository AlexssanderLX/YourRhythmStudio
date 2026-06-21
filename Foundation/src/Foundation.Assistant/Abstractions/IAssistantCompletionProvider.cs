using Foundation.Assistant.Models;
using Foundation.Core.Models;

namespace Foundation.Assistant.Abstractions;

public interface IAssistantCompletionProvider
{
    Task<OperationResult<AssistantCompletionResult>> CompleteAsync(
        AssistantCompletionRequest request,
        CancellationToken cancellationToken = default);
}
