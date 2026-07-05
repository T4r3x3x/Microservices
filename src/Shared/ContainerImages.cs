namespace Microservices.Infrastructure;

public static class ContainerImages
{
    public const string PostgresTag = "18.3";
    public const string PostgresImage = $"postgres:{PostgresTag}";

    public const string RedisTag = "8.6";
    public const string RedisImage = $"redis:{RedisTag}";

    public const string RabbitMqManagementTag = "4.3-management";
}
