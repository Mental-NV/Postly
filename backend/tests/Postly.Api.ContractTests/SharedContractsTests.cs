using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Postly.Api.ContractTests;

public class SharedContractsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SharedContractsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Unauthorized_Request_Returns_401_With_ProblemDetails()
    {
        var response = await _client.GetAsync("/api/timeline");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.Equal("application/problem+json", contentType);
    }

    [Fact]
    public async Task NotFound_Endpoint_Returns_404()
    {
        var response = await _client.GetAsync("/api/nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
