using System.Text.Json;
using Orders.Api.Features.Orders;

namespace Orders.Api.Infrastructure.Messaging;

public static class OutboxMessageFactory
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public static OutboxMessage CreateOrderCreated(
        OrderCreatedEvent @event,
        DateTimeOffset occurredAt) =>
        new(
            Guid.NewGuid(),
            nameof(OrderCreatedEvent),
            JsonSerializer.Serialize(@event, SerializerOptions),
            occurredAt);
}
