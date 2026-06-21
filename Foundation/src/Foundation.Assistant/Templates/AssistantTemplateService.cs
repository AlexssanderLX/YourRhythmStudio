using Foundation.Assistant.Models;

namespace Foundation.Assistant.Templates;

public sealed class AssistantTemplateService
{
    public string BuildOrderReviewMessage(OrderReviewMessageRequest request)
    {
        return
            $"Perfeito, {request.CustomerName}. O pedido foi recebido. Revise os dados aqui: {request.EditLink} " +
            $"Se precisar ajustar algo, voce pode editar por ate {request.EditWindowMinutes} minutos. " +
            $"Resumo do pedido: {request.OrderSummary}";
    }
}
