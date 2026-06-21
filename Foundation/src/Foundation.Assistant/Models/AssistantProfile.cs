namespace Foundation.Assistant.Models;

public sealed class AssistantProfile
{
    public string DisplayName { get; init; } = string.Empty;

    public string ToneInstructions { get; init; } = string.Empty;

    public string WelcomeMessage { get; init; } = string.Empty;

    public string FallbackMessage { get; init; } = "No momento nao consegui concluir a resposta com seguranca.";

    public string OrderRedirectMessage { get; init; } = string.Empty;

    public string? OrderLink { get; init; }

    public AssistantBusinessHours? BusinessHours { get; init; }

    public string ClosedMessage { get; init; } = "Agora a unidade esta fora do horario de atendimento.";

    public int RetainedMessageCount { get; init; } = 10;
}
