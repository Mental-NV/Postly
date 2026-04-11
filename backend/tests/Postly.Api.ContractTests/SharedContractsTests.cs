using System.Net;
using Xunit;

namespace Postly.Api.ContractTests;

public class SharedContractsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SharedContractsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Application_Responds_To_Requests()
    {
        // Baseline test: verify the application is running and responding
        var response = await _client.GetAsync("/");

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SPA_Fallback_Serves_Index_Html()
    {
        // Verify SPA fallback routing works
        var response = await _client.GetAsync("/some-spa-route");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("<div id=\"root\">", content);
    }
}
