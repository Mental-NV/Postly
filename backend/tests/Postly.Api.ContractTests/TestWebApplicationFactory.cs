using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Persistence;
using Xunit;

namespace Postly.Api.ContractTests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"test_{Guid.NewGuid():N}.db";
    private static readonly string AppProjectPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../../../src/Postly.Api"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseContentRoot(AppProjectPath);
        builder.UseWebRoot("wwwroot");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft"] = "Warning",
                ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Warning",
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace the database with a unique one for this test instance
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options
                    .UseSqlite($"Data Source={_dbName}")
                    .ConfigureWarnings(warnings =>
                    {
                        warnings.Ignore(SqliteEventId.TableRebuildPendingWarning);
                        warnings.Ignore(RelationalEventId.NonTransactionalMigrationOperationWarning);
                    });
            });
        });
    }

    public HttpClient CreateClientWithCookies()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    public void ResetData()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DataSeed.ResetAsync(dbContext).GetAwaiter().GetResult();
    }

    public async Task SignInAsBobAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/signin", new
        {
            username = "bob",
            password = "TestPassword123"
        });

        response.EnsureSuccessStatusCode();
    }

    public async Task<long> GetConversationPostIdAsync(string body = DataSeed.ConversationPostBody)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Posts
            .Where(post => post.Body == body)
            .Select(post => post.Id)
            .SingleAsync();
    }

    public static async Task<ProblemDetailsResponse> AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedType,
        string? expectedTitle = null,
        string? expectedDetailContains = null)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.NotNull(problem);
        Assert.Equal(expectedType, problem!.Type);
        Assert.Equal((int)expectedStatus, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.TraceId));

        if (expectedTitle != null)
        {
            Assert.Equal(expectedTitle, problem.Title);
        }

        if (expectedDetailContains != null)
        {
            Assert.Contains(expectedDetailContains, problem.Detail ?? string.Empty, StringComparison.Ordinal);
        }

        return problem;
    }

    public static async Task<ProblemDetailsResponse> AssertValidationProblemAsync(
        HttpResponseMessage response,
        string expectedField,
        string? expectedMessage = null)
    {
        var problem = await AssertProblemAsync(
            response,
            HttpStatusCode.BadRequest,
            ErrorCodes.ValidationFailed,
            "One or more validation errors occurred.");

        Assert.NotNull(problem.Errors);
        Assert.True(problem.Errors!.ContainsKey(expectedField));

        if (expectedMessage != null)
        {
            Assert.Contains(expectedMessage, problem.Errors[expectedField]);
        }

        return problem;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Clean up the test database
            if (File.Exists(_dbName))
            {
                File.Delete(_dbName);
            }
        }
        base.Dispose(disposing);
    }
}
