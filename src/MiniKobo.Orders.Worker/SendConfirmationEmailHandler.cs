using System.Globalization;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;

namespace MiniKobo.Orders.Worker;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 1025;
    public string From { get; init; } = "orders@minikobo.test";
}

/// <summary>Sends the confirmation email over SMTP (to Mailpit in development).</summary>
public sealed class SendConfirmationEmailHandler(IOptions<SmtpOptions> options, ILogger<SendConfirmationEmailHandler> logger)
    : IHandleMessages<SendConfirmationEmail>
{
    private static readonly CultureInfo Currency = CultureInfo.GetCultureInfo("en-CA");

    public async Task Handle(SendConfirmationEmail message, IMessageHandlerContext context)
    {
        var body = new StringBuilder()
            .AppendLine($"Hi {message.CustomerName},")
            .AppendLine()
            .AppendLine($"Thanks for your order {message.OrderId}. It's confirmed:")
            .AppendLine();
        foreach (var line in message.Lines)
        {
            body.AppendLine($"  {line.Quantity} x {line.Title} - {(line.UnitPrice * line.Quantity).ToString("C", Currency)}");
        }
        body.AppendLine().AppendLine($"Total: {message.Total.ToString("C", Currency)}").AppendLine().AppendLine("MiniKobo");

        var smtp = options.Value;
        using var client = new SmtpClient(smtp.Host, smtp.Port);
        using var mail = new MailMessage(smtp.From, message.CustomerEmail, "Your MiniKobo order is confirmed", body.ToString());
        await client.SendMailAsync(mail, context.CancellationToken);

        logger.LogInformation("Confirmation email sent for order {OrderId}", message.OrderId);
        await context.Reply(new ConfirmationEmailSent(message.OrderId));
    }
}
