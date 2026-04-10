using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Auth.Application;
using Postly.Api.Features.Auth.Endpoints;
using Postly.Api.Features.Posts.Application;
using Postly.Api.Features.Posts.Endpoints;
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
builder.Services.AddScoped<SigninHandler>();
builder.Services.AddScoped<SignoutHandler>();
builder.Services.AddScoped<GetSessionHandler>();
builder.Services.AddScoped<CreatePostHandler>();
builder.Services.AddScoped<UpdatePostHandler>();
builder.Services.AddScoped<DeletePostHandler>();

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
app.MapSigninEndpoints();
app.MapSessionEndpoints();
app.MapPostMutationEndpoints();

// SPA fallback
app.MapFallbackToFile("index.html");

app.Run();
