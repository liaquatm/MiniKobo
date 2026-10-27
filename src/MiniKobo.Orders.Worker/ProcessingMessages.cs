using MiniKobo.Contracts;

namespace MiniKobo.Orders.Worker;

// Messages used only inside order processing, so they live here rather than in MiniKobo.Contracts.

public sealed record ProcessPayment(Guid OrderId, decimal Amount, string CustomerEmail) : ICommand;

public sealed record PaymentSucceeded(Guid OrderId) : IMessage;

public sealed record PaymentFailed(Guid OrderId, string Reason) : IMessage;

public sealed record SendConfirmationEmail(
    Guid OrderId,
    string CustomerName,
    string CustomerEmail,
    decimal Total,
    IReadOnlyList<OrderLine> Lines) : ICommand;

public sealed record ConfirmationEmailSent(Guid OrderId) : IMessage;
