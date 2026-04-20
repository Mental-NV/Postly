using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Persistence;

namespace Postly.Api.IntegrationTests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"test_{Guid.NewGuid():N}.db";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
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
