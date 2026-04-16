using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Features.Posts.Contracts;
using Postly.Api.Features.Profiles.Contracts;
using Postly.Api.Features.Timeline.Contracts;
using Postly.Api.Persistence;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Postly.Api.IntegrationTests;

public class ProfileEditingFlowTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProfileEditingFlowTests()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        ResetData();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task PatchProfile_ValidUpdate_ReflectsAcrossProfileTimelineAndDirectPost()
    {
        await SignInAsBobAsync();

        var updateResponse = await _client.PatchAsJsonAsync("/api/profiles/me", new
        {
            displayName = "  Bob Renamed  ",
            bio = "Updated profile bio"
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var profileResponse = await _client.GetFromJsonAsync<ProfileResponse>("/api/profiles/bob");
        Assert.NotNull(profileResponse);
        Assert.Equal("Bob Renamed", profileResponse.Profile.DisplayName);
        Assert.Equal("Updated profile bio", profileResponse.Profile.Bio);

        var timelineResponse = await _client.GetFromJsonAsync<TimelineResponse>("/api/timeline");
        Assert.NotNull(timelineResponse);
        Assert.Contains(timelineResponse.Posts, post => post.AuthorUsername == "bob" && post.AuthorDisplayName == "Bob Renamed");

        var bobPostId = await GetBobPostIdAsync();
        var directPostConversation = await _client.GetFromJsonAsync<ConversationResponse>($"/api/posts/{bobPostId}");
        Assert.NotNull(directPostConversation);
        Assert.Equal("Bob Renamed", directPostConversation.Target.Post?.AuthorDisplayName);
    }

    [Fact]
    public async Task PutAvatar_ValidUpload_StoresNormalizedJpegAndProjectsVersionedAvatarUrl()
    {
        await SignInAsBobAsync();

        var response = await UploadAvatarAsync(await CreateTransparentPngAsync(320, 400));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await dbContext.UserAccounts.SingleAsync(account => account.Username == "bob");

        Assert.Equal("image/jpeg", user.AvatarContentType);
        Assert.NotNull(user.AvatarBytes);
        Assert.NotNull(user.AvatarUpdatedAtUtc);

        await using var normalizedStream = new MemoryStream(user.AvatarBytes!);
        var format = await Image.DetectFormatAsync(normalizedStream);
        Assert.NotNull(format);
        Assert.Equal("JPEG", format!.Name);

        normalizedStream.Position = 0;
        using var image = await Image.LoadAsync<Rgb24>(normalizedStream);
        Assert.Equal(512, image.Width);
        Assert.Equal(512, image.Height);

        var profileResponse = await _client.GetFromJsonAsync<ProfileResponse>("/api/profiles/bob");
        Assert.NotNull(profileResponse);
        Assert.True(profileResponse.Profile.HasCustomAvatar);
        Assert.Contains("/api/profiles/bob/avatar?v=", profileResponse.Profile.AvatarUrl, StringComparison.Ordinal);

        var timelineResponse = await _client.GetFromJsonAsync<TimelineResponse>("/api/timeline");
        Assert.NotNull(timelineResponse);
        Assert.Contains(timelineResponse.Posts, post =>
            post.AuthorUsername == "bob"
            && post.AuthorAvatarUrl == profileResponse.Profile.AvatarUrl);

        var bobPostId = await GetBobPostIdAsync();
        var directPostConversation = await _client.GetFromJsonAsync<ConversationResponse>($"/api/posts/{bobPostId}");
        Assert.NotNull(directPostConversation);
        Assert.Equal(profileResponse.Profile.AvatarUrl, directPostConversation.Target.Post?.AuthorAvatarUrl);
    }

    [Fact]
    public async Task PutAvatar_InvalidUpload_PreservesPreviousAvatarState()
    {
        await SignInAsBobAsync();

        var initialResponse = await UploadAvatarAsync(await CreateOpaquePngAsync(300, 300));
        Assert.Equal(HttpStatusCode.OK, initialResponse.StatusCode);

        byte[] originalBytes;
        DateTimeOffset? originalUpdatedAt;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await dbContext.UserAccounts.SingleAsync(account => account.Username == "bob");
            originalBytes = user.AvatarBytes!.ToArray();
            originalUpdatedAt = user.AvatarUpdatedAtUtc;
        }

        using var invalidContent = new MultipartFormDataContent();
        invalidContent.Add(new ByteArrayContent("not-an-image"u8.ToArray()), "avatar", "avatar.png");

        var invalidResponse = await _client.PutAsync("/api/profiles/me/avatar", invalidContent);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var verifiedUser = await verifyDbContext.UserAccounts.SingleAsync(account => account.Username == "bob");

        Assert.Equal(originalBytes, verifiedUser.AvatarBytes);
        Assert.Equal(originalUpdatedAt, verifiedUser.AvatarUpdatedAtUtc);
    }

    [Fact]
    public async Task PatchProfile_InvalidDisplayName_PreservesStoredIdentity()
    {
        await SignInAsBobAsync();

        var beforeResponse = await _client.GetFromJsonAsync<ProfileResponse>("/api/profiles/bob");
        Assert.NotNull(beforeResponse);

        var invalidResponse = await _client.PatchAsJsonAsync("/api/profiles/me", new
        {
            displayName = "   ",
            bio = "Attempted invalid update"
        });

        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

        var afterResponse = await _client.GetFromJsonAsync<ProfileResponse>("/api/profiles/bob");
        Assert.NotNull(afterResponse);
        Assert.Equal(beforeResponse.Profile.DisplayName, afterResponse.Profile.DisplayName);
        Assert.Equal(beforeResponse.Profile.Bio, afterResponse.Profile.Bio);
    }

    [Fact]
    public async Task PutAvatar_Asset001Upload_NormalizesTo512x512Jpeg()
    {
        await SignInAsBobAsync();

        var assetPath = FindAssetPath("001.jpg");
        var avatarBytes = await File.ReadAllBytesAsync(assetPath);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(avatarBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "avatar", "001.jpg");
        var response = await _client.PutAsync("/api/profiles/me/avatar", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await dbContext.UserAccounts.SingleAsync(account => account.Username == "bob");

        Assert.Equal("image/jpeg", user.AvatarContentType);
        Assert.NotNull(user.AvatarBytes);

        await using var normalizedStream = new MemoryStream(user.AvatarBytes!);
        using var image = await Image.LoadAsync<Rgb24>(normalizedStream);
        Assert.Equal(512, image.Width);
        Assert.Equal(512, image.Height);
    }

    private string FindAssetPath(string fileName)
    {
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "Postly.sln")))
        {
            currentDir = currentDir.Parent;
        }

        if (currentDir == null)
        {
            // Fallback for environments where the solution file isn't present in the expected hierarchy
            throw new FileNotFoundException($"Could not find project root (Postly.sln) from {Directory.GetCurrentDirectory()}");
        }

        var assetPath = Path.Combine(currentDir.FullName, "backend", "tests", "assets", "avatars", fileName);
        if (File.Exists(assetPath)) return assetPath;

        throw new FileNotFoundException($"Could not find asset {fileName} at {assetPath}");
    }

    private async Task SignInAsBobAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/signin", new
        {
            username = "bob",
            password = "TestPassword123"
        });

        response.EnsureSuccessStatusCode();
    }

    private async Task<long> GetBobPostIdAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Posts
            .Where(post => post.Author.Username == "bob" && post.ReplyToPostId == null)
            .Select(post => post.Id)
            .FirstAsync();
    }

    private async Task<HttpResponseMessage> UploadAvatarAsync(byte[] avatarBytes)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(avatarBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "avatar", "avatar.png");
        return await _client.PutAsync("/api/profiles/me/avatar", content);
    }

    private void ResetData()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DataSeed.ResetAsync(dbContext).GetAwaiter().GetResult();
    }

    private static async Task<byte[]> CreateTransparentPngAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(0, 0, 0, 0));
        for (var x = 25; x < width - 25; x++)
        {
            for (var y = 25; y < height - 25; y++)
            {
                image[x, y] = new Rgba32(0, 186, 124, 255);
            }
        }

        await using var stream = new MemoryStream();
        await image.SaveAsync(stream, new PngEncoder());
        return stream.ToArray();
    }

    private static async Task<byte[]> CreateOpaquePngAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(15, 20, 25, 255));

        await using var stream = new MemoryStream();
        await image.SaveAsync(stream, new PngEncoder());
        return stream.ToArray();
    }
}
