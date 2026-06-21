namespace Foundation.Assistant.Models;

public sealed record OutboundConversationMessage(
    string ConversationId,
    string Channel,
    string ContactId,
    string Text,
    DateTime CreatedAtUtc);
