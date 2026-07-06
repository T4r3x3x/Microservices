using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Api.Domain.Orders;

namespace Orders.Api.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.Property<Guid>("Id")
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.HasKey("Id");

        builder.Property<Guid>("OrderId")
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(item => item.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(item => item.ProductName)
            .HasColumnName("product_name")
            .HasMaxLength(OrderConstraints.ProductNameMaxLength)
            .IsRequired();

        builder.Property(item => item.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(
                OrderConstraints.MoneyPrecision,
                OrderConstraints.MoneyScale)
            .IsRequired();

        builder.Property(item => item.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Ignore(item => item.TotalPrice);

        builder.HasIndex("OrderId", nameof(OrderItem.ProductId))
            .IsUnique();
    }
}
