namespace Foundation.Assistant.Models;

public sealed record AssistantCompletionResult(
    string Text,
    bool UsedFallback,
    string ProviderName);
