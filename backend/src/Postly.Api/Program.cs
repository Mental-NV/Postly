using Microsoft.EntityFrameworkCore;
using Postly.Api.Persistence;
using Postly.Api.Security;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=postly.db";
    options.UseSqlite(connectionString);
});

// Authentication & Authorization
builder.Services.AddSessionCookieAuthentication();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentViewerAccessor, CurrentViewerAccessor>();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 5,
                QueueLimit = 0
            }));

    options.AddPolicy("write", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 20,
                QueueLimit = 0
            }));
});

// Problem Details
builder.Services.AddProblemDetails();

var app = builder.Build();

// Apply migrations and seed data in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    await DataSeed.SeedAsync(dbContext);
}

// Middleware pipeline
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// SPA fallback
app.MapFallbackToFile("index.html");

app.Run();
