using Amazon.Lambda.Core;
using FCG.Lambda.Payment.Contracts;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FCG.Lambda.Payment;

public class Function
{
    public Task<PaymentProcessedEvent> FunctionHandler(OrderPlacedEvent order, ILambdaContext context)
    {
        var (success, statusMessage) = SimulatePayment(order, context);

        var result = new PaymentProcessedEvent
        {
            OrderId = order.OrderId,
            UserId = order.UserId,
            GameId = order.GameId,
            GameTitle = order.GameTitle,
            UserEmail = order.UserEmail,
            Status = success ? PaymentStatus.Approved : PaymentStatus.Rejected,
            Message = statusMessage,
            ProcessedAt = DateTime.UtcNow
        };

        return Task.FromResult(result);
    }

    public static (bool Success, string Message) SimulatePayment(OrderPlacedEvent order, ILambdaContext context)
    {
        var success = order.Price > 0;
        var status = success ? "APPROVED" : "DECLINED";
        var message = success
            ? $"Payment approved for order {order.OrderId}"
            : $"Payment declined for order {order.OrderId}";

        context.Logger.LogInformation(
            $"\n========== PAYMENT PROCESSING ==========\n" +
            $"Order ID  : {order.OrderId}\n" +
            $"Game      : {order.GameTitle}\n" +
            $"Amount    : {order.Price:C}\n" +
            $"User      : {order.UserEmail}\n" +
            $"Card      : ****1111\n" +
            $"Status    : {status}\n" +
            $"========================================\n");

        return (success, message);
    }
}
