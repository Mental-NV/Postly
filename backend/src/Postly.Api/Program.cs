using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Auth.Application;
using Postly.Api.Features.Auth.Endpoints;
using Postly.Api.Persistence;
using Postly.Api.Security;

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

// Handlers
builder.Services.AddScoped<SignupHandler>();

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
app.UseAuthentication();
app.UseAuthorization();

// API endpoints
app.MapSignupEndpoints();

// SPA fallback
app.MapFallbackToFile("index.html");

app.Run();
