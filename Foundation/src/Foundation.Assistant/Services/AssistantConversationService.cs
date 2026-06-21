using Foundation.Assistant.Abstractions;
using Foundation.Assistant.Models;
using Foundation.Assistant.Templates;
using Foundation.Core.Abstractions;
using Foundation.Core.Models;

namespace Foundation.Assistant.Services;

public sealed class AssistantConversationService
{
    private readonly IAssistantCompletionProvider _completionProvider;
    private readonly IConversationStore _conversationStore;
    private readonly IMessageChannelClient _channelClient;
    private readonly AssistantTemplateService _templateService;
    private readonly IClock _clock;

    public AssistantConversationService(
        IAssistantCompletionProvider completionProvider,
        IConversationStore conversationStore,
        IMessageChannelClient channelClient,
        AssistantTemplateService templateService,
        IClock clock)
    {
        _completionProvider = completionProvider;
        _conversationStore = conversationStore;
        _channelClient = channelClient;
        _templateService = templateService;
        _clock = clock;
    }

    public async Task<OperationResult<AssistantCompletionResult>> HandleInboundAsync(
        AssistantProfile profile,
        InboundConversationMessage message,
        CancellationToken cancellationToken = default)
    {
        await _conversationStore.AppendAsync(
            message.ConversationId,
            new ConversationTurn("user", message.Text, message.ReceivedAtUtc),
            profile.RetainedMessageCount,
            cancellationToken);

        if (profile.BusinessHours is not null)
        {
            var currentTime = TimeOnly.FromDateTime(_clock.UtcNow);
            if (!profile.BusinessHours.IsOpenAt(currentTime))
            {
                var closedReply = new AssistantCompletionResult(profile.ClosedMessage, true, "hours-guard");
                await SendAndPersistReplyAsync(profile, message, closedReply, cancellationToken);
                return OperationResult<AssistantCompletionResult>.Success(closedReply);
            }
        }

        var history = await _conversationStore.GetRecentAsync(message.ConversationId, profile.RetainedMessageCount, cancellationToken);
        var response = await _completionProvider.CompleteAsync(
            new AssistantCompletionRequest(profile, history, message),
            cancellationToken);

        if (response.IsFailure || response.Value is null)
        {
            var fallback = new AssistantCompletionResult(profile.FallbackMessage, true, "fallback");
            await SendAndPersistReplyAsync(profile, message, fallback, cancellationToken);
            return OperationResult<AssistantCompletionResult>.Success(fallback);
        }

        await SendAndPersistReplyAsync(profile, message, response.Value, cancellationToken);
        return response;
    }

    public async Task SendOrderReviewAsync(OrderReviewMessageRequest request, CancellationToken cancellationToken = default)
    {
        var text = _templateService.BuildOrderReviewMessage(request);
        await _channelClient.SendAsync(
            new OutboundConversationMessage(request.ContactId, request.Channel, request.ContactId, text, _clock.UtcNow),
            cancellationToken);
    }

    private async Task SendAndPersistReplyAsync(
        AssistantProfile profile,
        InboundConversationMessage message,
        AssistantCompletionResult result,
        CancellationToken cancellationToken)
    {
        var outbound = new OutboundConversationMessage(
            message.ConversationId,
            message.Channel,
            message.ContactId,
            result.Text,
            _clock.UtcNow);

        await _channelClient.SendAsync(outbound, cancellationToken);
        await _conversationStore.AppendAsync(
            message.ConversationId,
            new ConversationTurn("assistant", result.Text, outbound.CreatedAtUtc),
            profile.RetainedMessageCount,
            cancellationToken);
    }
}
