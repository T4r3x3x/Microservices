using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cart.IntegrationTests;

public sealed class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public ApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"))
            .CreateClient();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/health")]
    [InlineData("/alive")]
    [InlineData("/openapi/v1.json")]
    public async Task EndpointReturnsSuccess(string path)
    {
        using var response = await client.GetAsync(path, CancellationToken.None);

        response.EnsureSuccessStatusCode();
    }
}
