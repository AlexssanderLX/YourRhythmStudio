namespace Foundation.Assistant.Models;

public sealed record AssistantCompletionRequest(
    AssistantProfile Profile,
    IReadOnlyCollection<ConversationTurn> History,
    InboundConversationMessage Message);
