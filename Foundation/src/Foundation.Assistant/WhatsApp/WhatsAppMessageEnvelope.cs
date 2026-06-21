namespace Foundation.Assistant.WhatsApp;

public sealed record WhatsAppMessageEnvelope(
    string PhoneNumber,
    string Text,
    DateTime ReceivedAtUtc);
