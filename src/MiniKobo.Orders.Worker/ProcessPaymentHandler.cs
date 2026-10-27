namespace MiniKobo.Orders.Worker;

/// <summary>
/// Simulated payment provider. An email containing "+fail" (e.g. you+fail@example.com) is declined,
/// so the compensation path can be demonstrated on demand.
/// </summary>
public sealed class ProcessPaymentHandler(ILogger<ProcessPaymentHandler> logger) : IHandleMessages<ProcessPayment>
{
    public async Task Handle(ProcessPayment message, IMessageHandlerContext context)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), context.CancellationToken); // pretend to call a provider

        if (message.CustomerEmail.Contains("+fail", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Payment declined for order {OrderId}", message.OrderId);
            await context.Reply(new PaymentFailed(message.OrderId, "Payment was declined (simulated)."));
            return;
        }

        logger.LogInformation("Payment of {Amount} taken for order {OrderId}", message.Amount, message.OrderId);
        await context.Reply(new PaymentSucceeded(message.OrderId));
    }
}
