using MiniKobo.Contracts;

namespace MiniKobo.Orders.Worker;

public sealed class OrderProcessingSagaData : ContainSagaData
{
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public List<OrderLine> Lines { get; set; } = [];
}

/// <summary>
/// Coordinates one order: reserve stock -> take payment -> send confirmation.
/// Its state is saved between messages, and failed steps are undone (compensation).
/// </summary>
public sealed class OrderProcessingSaga(ILogger<OrderProcessingSaga> logger) : Saga<OrderProcessingSagaData>,
    IAmStartedByMessages<OrderPlaced>,
    IHandleMessages<StockReserved>,
    IHandleMessages<StockReservationFailed>,
    IHandleMessages<PaymentSucceeded>,
    IHandleMessages<PaymentFailed>,
    IHandleMessages<ConfirmationEmailSent>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderProcessingSagaData> mapper) =>
        mapper.MapSaga(saga => saga.OrderId)
            .ToMessage<OrderPlaced>(message => message.OrderId)
            .ToMessage<StockReserved>(message => message.OrderId)
            .ToMessage<StockReservationFailed>(message => message.OrderId)
            .ToMessage<PaymentSucceeded>(message => message.OrderId)
            .ToMessage<PaymentFailed>(message => message.OrderId)
            .ToMessage<ConfirmationEmailSent>(message => message.OrderId);

    public async Task Handle(OrderPlaced message, IMessageHandlerContext context)
    {
        Data.CustomerName = message.CustomerName;
        Data.CustomerEmail = message.CustomerEmail;
        Data.Total = message.Total;
        Data.Lines = message.Lines.ToList();

        logger.LogInformation("Processing order {OrderId}: reserving stock", Data.OrderId);
        await context.Send(new ReserveStock(
            Data.OrderId,
            Data.Lines.Select(line => new StockLine(line.Isbn, line.Quantity)).ToList()));
    }

    public async Task Handle(StockReserved message, IMessageHandlerContext context)
    {
        await PublishStatus(context, OrderStatus.StockReserved);
        await context.SendLocal(new ProcessPayment(Data.OrderId, Data.Total, Data.CustomerEmail));
    }

    public async Task Handle(StockReservationFailed message, IMessageHandlerContext context)
    {
        // Nothing was reserved, so there is nothing to undo.
        await PublishStatus(context, OrderStatus.Failed, message.Reason);
        MarkAsComplete();
    }

    public async Task Handle(PaymentSucceeded message, IMessageHandlerContext context)
    {
        await PublishStatus(context, OrderStatus.Paid);
        await context.SendLocal(new SendConfirmationEmail(
            Data.OrderId, Data.CustomerName, Data.CustomerEmail, Data.Total, Data.Lines));
    }

    public async Task Handle(PaymentFailed message, IMessageHandlerContext context)
    {
        // Compensation: stock was reserved in the previous step, so give it back.
        await context.Send(new ReleaseStock(Data.OrderId));
        await PublishStatus(context, OrderStatus.Failed, message.Reason);
        MarkAsComplete();
    }

    public async Task Handle(ConfirmationEmailSent message, IMessageHandlerContext context)
    {
        await PublishStatus(context, OrderStatus.Confirmed);
        MarkAsComplete();
    }

    private Task PublishStatus(IMessageHandlerContext context, OrderStatus status, string? reason = null)
    {
        logger.LogInformation("Order {OrderId} -> {Status}", Data.OrderId, status);
        return context.Publish(new OrderStatusChanged(Data.OrderId, status, reason));
    }
}
