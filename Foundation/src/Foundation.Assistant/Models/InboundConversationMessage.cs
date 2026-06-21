namespace Foundation.Assistant.Models;

public sealed record InboundConversationMessage(
    string ConversationId,
    string Channel,
    string ContactId,
    string ContactDisplayName,
    string Text,
    DateTime ReceivedAtUtc);
