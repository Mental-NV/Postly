using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Persistence;

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
                options.UseSqlite($"Data Source={_dbName}");
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
