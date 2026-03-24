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
    private readonly string _paymentProcessedQueueUrl;

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public Function() : this(new AmazonSQSClient()) { }

    public Function(IAmazonSQS sqsClient)
    {
        _sqsClient = sqsClient;
        _paymentProcessedQueueUrl = Environment.GetEnvironmentVariable("PAYMENT_PROCESSED_QUEUE_URL")
            ?? throw new InvalidOperationException("PAYMENT_PROCESSED_QUEUE_URL not configured");
    }

    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        foreach (var message in evnt.Records)
        {
            await ProcessMessageAsync(message, context);
        }
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        context.Logger.LogInformation($"Processing message {message.MessageId}");

        OrderPlacedEvent? order;
        try
        {
            order = JsonSerializer.Deserialize<OrderPlacedEvent>(message.Body, JsonOptions);
        }
        catch (JsonException ex)
        {
            context.Logger.LogWarning($"Failed to deserialize OrderPlacedEvent from message {message.MessageId}: {ex.Message}");
            return;
        }

        if (order is null)
        {
            context.Logger.LogWarning($"Null payload in message {message.MessageId}");
            return;
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
            QueueUrl = _paymentProcessedQueueUrl,
            MessageBody = JsonSerializer.Serialize(result, JsonOptions)
        });

        context.Logger.LogInformation(
            $"Payment {result.Status} for Order {order.OrderId} — published to payment-processed queue");
    }

    public static (bool Success, string Message) SimulatePayment(OrderPlacedEvent order, ILambdaContext context)
    {
        // Rejeita se price for zero; caso contrário aprova (simulação)
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
