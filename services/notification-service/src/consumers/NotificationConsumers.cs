using MassTransit;
using Shared.Events;

namespace NotificationService.Consumers;

/// <summary>
/// CONCEPT: MassTransit Consumer — this class "listens" to a specific event type.
/// When reward-service publishes PointsAssignedEvent to RabbitMQ, MassTransit
/// automatically routes it here and calls Consume().
///
/// Key benefit: reward-service doesn't know notification-service exists.
/// It just publishes an event. notification-service subscribes independently.
/// This is "loose coupling" in practice.
/// </summary>
public class PointsAssignedConsumer(ILogger<PointsAssignedConsumer> logger)
    : IConsumer<PointsAssignedEvent>
{
    public async Task Consume(ConsumeContext<PointsAssignedEvent> context)
    {
        var evt = context.Message;

        // In production: call your email/SMS provider here (SendGrid, Twilio, etc.)
        // For learning: we log the notification so you can see it in docker logs.
        logger.LogInformation(
            "[NOTIFICATION] 🎉 Points assigned to user {UserId}: +{Points} points | Reason: {Reason} | Transaction: {TxId}",
            evt.UserId, evt.Points, evt.Reason, evt.TransactionId);

        // Simulate async notification (e.g. calling SendGrid API)
        await Task.Delay(100);

        logger.LogInformation(
            "[NOTIFICATION] ✅ Email sent: 'You earned {Points} points for: {Reason}'",
            evt.Points, evt.Reason);
    }
}

/// <summary>Listens for point redemption events.</summary>
public class PointsRedeemedConsumer(ILogger<PointsRedeemedConsumer> logger)
    : IConsumer<PointsRedeemedEvent>
{
    public async Task Consume(ConsumeContext<PointsRedeemedEvent> context)
    {
        var evt = context.Message;

        logger.LogInformation(
            "[NOTIFICATION] 💳 Points redeemed by user {UserId}: {Points} points → ${Amount:F2}",
            evt.UserId, evt.PointsRedeemed, evt.MoneyAmount);

        await Task.Delay(100);

        logger.LogInformation(
            "[NOTIFICATION] ✅ Email sent: 'You redeemed {Points} points for ${Amount:F2} wallet credit'",
            evt.PointsRedeemed, evt.MoneyAmount);
    }
}

/// <summary>Listens for wallet credit events.</summary>
public class WalletCreditedConsumer(ILogger<WalletCreditedConsumer> logger)
    : IConsumer<WalletCreditedEvent>
{
    public async Task Consume(ConsumeContext<WalletCreditedEvent> context)
    {
        var evt = context.Message;

        logger.LogInformation(
            "[NOTIFICATION] 💰 Wallet credited for user {UserId}: +${Amount:F2} | New balance: ${Balance:F2}",
            evt.UserId, evt.AmountCredited, evt.NewBalance);

        await Task.Delay(100);

        logger.LogInformation(
            "[NOTIFICATION] ✅ Push notification sent: 'Your wallet has been credited ${Amount:F2}! New balance: ${Balance:F2}'",
            evt.AmountCredited, evt.NewBalance);
    }
}
