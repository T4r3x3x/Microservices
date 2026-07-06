namespace Orders.Api.Infrastructure.Messaging;

public static class OrderMessagingTopology
{
    public const string ExchangeName = "orders";
    public const string OrderCreatedRoutingKey = "order.created";
}
