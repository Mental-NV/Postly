using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Persistence;
using Xunit;

namespace Postly.Api.IntegrationTests;

public class StartupAndHostingTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public StartupAndHostingTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Application_Starts_Successfully()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/");

        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Database_Migrations_Are_Applied()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var canConnect = await dbContext.Database.CanConnectAsync();
        Assert.True(canConnect);

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        Assert.Empty(pendingMigrations);
    }

    [Fact]
    public async Task DataSeed_Creates_Test_Users()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var bob = await dbContext.UserAccounts
            .FirstOrDefaultAsync(u => u.NormalizedUsername == "BOB");
        var alice = await dbContext.UserAccounts
            .FirstOrDefaultAsync(u => u.NormalizedUsername == "ALICE");

        Assert.NotNull(bob);
        Assert.NotNull(alice);
        Assert.Equal("Bob Tester", bob.DisplayName);
        Assert.Equal("Alice Example", alice.DisplayName);
    }

    [Fact]
    public async Task Static_Files_Are_Served()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/");

        Assert.True(response.IsSuccessStatusCode);
    }
}
