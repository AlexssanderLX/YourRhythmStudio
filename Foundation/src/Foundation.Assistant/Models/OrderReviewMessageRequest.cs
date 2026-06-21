namespace Foundation.Assistant.Models;

public sealed record OrderReviewMessageRequest(
    string CustomerName,
    string ContactId,
    string Channel,
    string OrderSummary,
    string EditLink,
    int EditWindowMinutes);
