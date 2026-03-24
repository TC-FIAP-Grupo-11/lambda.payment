using System.Text.Json;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using Amazon.SQS;
using Amazon.SQS.Model;
using FCG.Lambda.Payment.Contracts;
using Moq;
using Xunit;

namespace FCG.Lambda.Payment.Tests;

public class FunctionTest
{
    private readonly Mock<IAmazonSQS> _sqsMock;
    private readonly Function _function;

    public FunctionTest()
    {
        Environment.SetEnvironmentVariable("PAYMENT_PROCESSED_QUEUE_URL", "https://sqs.us-east-1.amazonaws.com/123/fcg-payment-processed");
        _sqsMock = new Mock<IAmazonSQS>();
        _sqsMock
            .Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), default))
            .ReturnsAsync(new SendMessageResponse());
        _function = new Function(_sqsMock.Object);
    }

    [Fact]
    public async Task ApprovedPayment_WhenPriceIsPositive()
    {
        var order = new OrderPlacedEvent
        {
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            GameTitle = "Test Game",
            UserEmail = "user@test.com",
            Price = 49.99m,
            PlacedAt = DateTime.UtcNow
        };

        var sqsEvent = BuildSqsEvent(order);
        var context = new TestLambdaContext { Logger = new TestLambdaLogger() };

        await _function.FunctionHandler(sqsEvent, context);

        _sqsMock.Verify(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(r => r.MessageBody.Contains("Approved")),
            default), Times.Once);
    }

    [Fact]
    public async Task RejectedPayment_WhenPriceIsZero()
    {
        var order = new OrderPlacedEvent
        {
            OrderId = Guid.NewGuid(),
            GameTitle = "Free Game",
            UserEmail = "user@test.com",
            Price = 0m,
            PlacedAt = DateTime.UtcNow
        };

        var sqsEvent = BuildSqsEvent(order);
        var context = new TestLambdaContext { Logger = new TestLambdaLogger() };

        await _function.FunctionHandler(sqsEvent, context);

        _sqsMock.Verify(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(r => r.MessageBody.Contains("Rejected")),
            default), Times.Once);
    }

    [Fact]
    public async Task InvalidMessage_IsSkippedGracefully()
    {
        var sqsEvent = new SQSEvent
        {
            Records = [new SQSEvent.SQSMessage { MessageId = "bad-msg", Body = "not-valid-json" }]
        };
        var context = new TestLambdaContext { Logger = new TestLambdaLogger() };

        await _function.FunctionHandler(sqsEvent, context);

        _sqsMock.Verify(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), default), Times.Never);
    }

    private static SQSEvent BuildSqsEvent(OrderPlacedEvent order) => new()
    {
        Records = [new SQSEvent.SQSMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            Body = JsonSerializer.Serialize(order)
        }]
    };
}
