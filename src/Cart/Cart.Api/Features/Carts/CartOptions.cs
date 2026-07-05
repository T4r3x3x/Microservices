using System.ComponentModel.DataAnnotations;

namespace Cart.Api.Features.Carts;

public sealed class CartOptions
{
    public const string SectionName = "Cart";

    [Range(typeof(TimeSpan), "00:01:00", "365.00:00:00")]
    public TimeSpan TimeToLive { get; init; } = TimeSpan.FromDays(30);
}
