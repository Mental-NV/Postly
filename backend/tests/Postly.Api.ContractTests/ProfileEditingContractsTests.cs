using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Postly.Api.Features.Profiles.Contracts;
using Postly.Api.Persistence;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Postly.Api.ContractTests;

public class ProfileEditingContractsTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProfileEditingContractsTests()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
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
    public async Task PatchProfileMe_WhenAnonymous_Returns401ProblemDetails()
    {
        var response = await _factory.CreateClient().PatchAsJsonAsync("/api/profiles/me", new
        {
            displayName = "Bob Updated",
            bio = "Updated bio"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PutProfileAvatar_WhenAnonymous_Returns401ProblemDetails()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(await CreatePngAsync(300, 300)), "avatar", "avatar.png");

        var response = await _factory.CreateClient().PutAsync("/api/profiles/me/avatar", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PutProfileAvatar_WithInvalidUpload_Returns400ProblemDetails()
    {
        await SignInAsBobAsync();

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent("<svg></svg>"u8.ToArray()), "avatar", "avatar.svg");

        var response = await _client.PutAsync("/api/profiles/me/avatar", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PublicProfileReadsRemainAvailable_AndAvatarEndpointServesJpeg()
    {
        await SignInAsBobAsync();

        var assetPath = FindAssetPath("001.jpg");
        var avatarBytes = await File.ReadAllBytesAsync(assetPath);
        var avatarResponse = await UploadAvatarAsync(avatarBytes, "image/jpeg", "001.jpg");
        Assert.Equal(HttpStatusCode.OK, avatarResponse.StatusCode);

        var anonymousClient = _factory.CreateClient();

        var profileResponse = await anonymousClient.GetAsync("/api/profiles/bob");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(profile);
        Assert.True(profile.Profile.HasCustomAvatar);
        Assert.NotNull(profile.Profile.AvatarUrl);
        Assert.Contains("/api/profiles/bob/avatar?v=", profile.Profile.AvatarUrl, StringComparison.Ordinal);

        var publicAvatarResponse = await anonymousClient.GetAsync("/api/profiles/bob/avatar");
        Assert.Equal(HttpStatusCode.OK, publicAvatarResponse.StatusCode);
        Assert.Equal("image/jpeg", publicAvatarResponse.Content.Headers.ContentType?.MediaType);
    }

    private string FindAssetPath(string fileName)
    {
        var currentDir = Directory.GetCurrentDirectory();
        var pathsToTry = new[]
        {
            Path.Combine(currentDir, "..", "..", "..", "backend", "tests", "assets", "avatars", fileName),
            Path.Combine(currentDir, "..", "..", "..", "assets", "avatars", fileName),
            Path.Combine(currentDir, "assets", "avatars", fileName),
            "/home/mental/projects/Postly/backend/tests/assets/avatars/" + fileName
        };

        foreach (var path in pathsToTry)
        {
            if (File.Exists(path)) return path;
        }

        throw new FileNotFoundException($"Could not find asset {fileName}");
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

    private async Task<HttpResponseMessage> UploadAvatarAsync(byte[] avatarBytes, string contentType = "image/png", string fileName = "avatar.png")
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(avatarBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "avatar", fileName);

        return await _client.PutAsync("/api/profiles/me/avatar", content);
    }

    private void ResetData()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DataSeed.ResetAsync(dbContext).GetAwaiter().GetResult();
    }

    private static async Task<byte[]> CreatePngAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(29, 155, 240, 255));
        await using var stream = new MemoryStream();
        await image.SaveAsync(stream, new PngEncoder());
        return stream.ToArray();
    }
}
