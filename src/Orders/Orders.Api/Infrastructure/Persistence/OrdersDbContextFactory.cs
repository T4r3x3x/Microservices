using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Orders.Api.Infrastructure.Persistence;

public sealed class OrdersDbContextFactory
    : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<OrdersDbContext> optionsBuilder = new();
        string connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__orders-db")
            ?? "Host=localhost;Database=orders;Username=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new OrdersDbContext(optionsBuilder.Options);
    }
}
