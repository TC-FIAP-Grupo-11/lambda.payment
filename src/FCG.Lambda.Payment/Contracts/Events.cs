namespace FCG.Lambda.Payment.Contracts;

public record OrderPlacedEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public string GameTitle { get; init; } = string.Empty;
    public string UserEmail { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public DateTime PlacedAt { get; init; }
}

public record PaymentProcessedEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public string GameTitle { get; init; } = string.Empty;
    public string UserEmail { get; init; } = string.Empty;
    public PaymentStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime ProcessedAt { get; init; }
}

public enum PaymentStatus
{
    Approved = 1,
    Rejected = 2
}
