using Foundation.Assistant.Abstractions;
using Foundation.Assistant.Models;
using Foundation.Core.Models;

namespace Foundation.Assistant.Ai;

public sealed class RuleBasedAssistantCompletionProvider : IAssistantCompletionProvider
{
    public Task<OperationResult<AssistantCompletionResult>> CompleteAsync(
        AssistantCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var lower = request.Message.Text.Trim().ToLowerInvariant();
        var answer = lower switch
        {
            var text when string.IsNullOrWhiteSpace(text) => request.Profile.FallbackMessage,
            var text when text.Contains("pedido") && !string.IsNullOrWhiteSpace(request.Profile.OrderLink) =>
                $"{request.Profile.WelcomeMessage} {request.Profile.OrderRedirectMessage} {request.Profile.OrderLink}",
            var text when text.Contains("oi") || text.Contains("olá") || text.Contains("ola") =>
                $"{request.Profile.WelcomeMessage} {request.Profile.OrderRedirectMessage}".Trim(),
            _ => $"{request.Profile.WelcomeMessage} {request.Profile.FallbackMessage}".Trim()
        };

        return Task.FromResult(OperationResult<AssistantCompletionResult>.Success(
            new AssistantCompletionResult(answer, false, "rule-based")));
    }
}
