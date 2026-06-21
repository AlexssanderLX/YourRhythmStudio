namespace Foundation.Assistant.Models;

public sealed record ConversationTurn(
    string Role,
    string Content,
    DateTime OccurredAtUtc);
