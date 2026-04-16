using Microsoft.EntityFrameworkCore;
using Postly.Api.Features.Auth.Application;
using Postly.Api.Features.Auth.Endpoints;
using Postly.Api.Features.Notifications.Application;
using Postly.Api.Features.Notifications.Endpoints;
using Postly.Api.Features.Posts.Application;
using Postly.Api.Features.Posts.Endpoints;
using Postly.Api.Features.Profiles.Application;
using Postly.Api.Features.Profiles.Endpoints;
using Postly.Api.Features.Timeline.Application;
using Postly.Api.Features.Timeline.Endpoints;
using Postly.Api.Persistence;
using Postly.Api.Security;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = DefaultConnectionString.Resolve(builder.Configuration);
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
builder.Services.AddScoped<GetPostHandler>();
builder.Services.AddScoped<GetRepliesHandler>();
builder.Services.AddScoped<CreateReplyHandler>();
builder.Services.AddScoped<LikePostHandler>();
builder.Services.AddScoped<UnlikePostHandler>();
builder.Services.AddScoped<GetTimelineHandler>();
builder.Services.AddScoped<GetProfileHandler>();
builder.Services.AddScoped<UpdateProfileHandler>();
builder.Services.AddScoped<ReplaceAvatarHandler>();
builder.Services.AddScoped<ProfileAvatarProcessor>();
builder.Services.AddScoped<FollowUserHandler>();
builder.Services.AddScoped<UnfollowUserHandler>();
builder.Services.AddScoped<GetNotificationsHandler>();
builder.Services.AddScoped<OpenNotificationHandler>();

// Problem Details
builder.Services.AddProblemDetails();

var app = builder.Build();

// Apply migrations on local/test runs. Keep production Azure schema changes in CD.
if (!DefaultConnectionString.IsAzureAppService())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    
    if (app.Environment.IsDevelopment())
    {
        await DataSeed.SeedAsync(dbContext);
    }
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
app.MapPostQueryEndpoints();
app.MapPostInteractionEndpoints();
app.MapTimelineEndpoints();
app.MapProfileEndpoints();
app.MapNotificationEndpoints();

// SPA fallback
app.MapFallbackToFile("index.html");

app.Run();
