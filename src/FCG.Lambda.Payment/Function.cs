using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using Amazon.SQS.Model;
using FCG.Lambda.Payment.Contracts;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FCG.Lambda.Payment;

public class Function
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;

    public Function() : this(new AmazonSQSClient()) { }

    public Function(IAmazonSQS sqsClient)
    {
        _sqsClient = sqsClient;
        _queueUrl = Environment.GetEnvironmentVariable("PAYMENT_PROCESSED_QUEUE_URL") ?? string.Empty;
    }

    public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        foreach (var record in sqsEvent.Records)
        {
            OrderPlacedEvent? order;
            try
            {
                order = JsonSerializer.Deserialize<OrderPlacedEvent>(record.Body);
                if (order is null) continue;
            }
            catch (JsonException)
            {
                context.Logger.LogWarning($"Skipping invalid message {record.MessageId}");
                continue;
            }

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

            await _sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = JsonSerializer.Serialize(result)
            });
        }
    }

    public static (bool Success, string Message) SimulatePayment(OrderPlacedEvent order, ILambdaContext context)
    {
        var success = order.Price > 0;
        var status = success ? "APPROVED" : "DECLINED";
        var message = success
            ? $"Payment Approved for order {order.OrderId}"
            : $"Payment Rejected for order {order.OrderId}";

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
