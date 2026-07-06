using System.Text;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Infrastructure.Persistence;
using RabbitMQ.Client;

namespace Orders.Api.Infrastructure.Messaging;

public sealed partial class OutboxPublisherWorker(
    IServiceScopeFactory serviceScopeFactory,
    IConnection connection,
    TimeProvider timeProvider,
    ILogger<OutboxPublisherWorker> logger) : BackgroundService
{
    private const int BatchSize = 20;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using IChannel channel =
            await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.ExchangeDeclareAsync(
            OrderMessagingTopology.ExchangeName,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        using PeriodicTimer timer = new(PollingInterval, timeProvider);

        do
        {
            await PublishBatchAsync(channel, stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PublishBatchAsync(
        IChannel channel,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope =
            serviceScopeFactory.CreateAsyncScope();
        OrdersDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        List<OutboxMessage> messages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.OccurredAt)
            .ThenBy(message => message.Id)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (OutboxMessage message in messages)
        {
            await PublishMessageAsync(channel, message, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task PublishMessageAsync(
        IChannel channel,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            BasicProperties properties = new()
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = message.Id.ToString(),
                Type = message.Type
            };
            byte[] body = Encoding.UTF8.GetBytes(message.Content);

            await channel.BasicPublishAsync(
                OrderMessagingTopology.ExchangeName,
                OrderMessagingTopology.OrderCreatedRoutingKey,
                mandatory: false,
                basicProperties: properties,
                body,
                cancellationToken);

            message.MarkProcessed(timeProvider.GetUtcNow());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogOutboxPublishFailed(logger, exception, message.Id);
            message.MarkFailed(TruncateError(exception.Message));
        }
    }

    private static string TruncateError(string error) =>
        error.Length <= 2000
            ? error
            : error[..2000];

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Failed to publish outbox message {MessageId}.")]
    private static partial void LogOutboxPublishFailed(
        ILogger logger,
        Exception exception,
        Guid messageId);
}
