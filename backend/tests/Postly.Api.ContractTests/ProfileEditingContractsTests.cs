using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Postly.Api.Features.Shared.Errors;
using Postly.Api.Features.Profiles.Contracts;
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
        _client = _factory.CreateClientWithCookies();
        _factory.ResetData();
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

        await TestWebApplicationFactory.AssertProblemAsync(
            response,
            HttpStatusCode.Unauthorized,
            ErrorCodes.Unauthorized,
            "Authentication is required.");
    }

    [Fact]
    public async Task PutProfileAvatar_WhenAnonymous_Returns401ProblemDetails()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(await CreatePngAsync(300, 300)), "avatar", "avatar.png");

        var response = await _factory.CreateClient().PutAsync("/api/profiles/me/avatar", content);

        await TestWebApplicationFactory.AssertProblemAsync(
            response,
            HttpStatusCode.Unauthorized,
            ErrorCodes.Unauthorized,
            "Authentication is required.");
    }

    [Fact]
    public async Task PutProfileAvatar_WithInvalidUpload_Returns400ProblemDetails()
    {
        await _factory.SignInAsBobAsync(_client);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent("<svg></svg>"u8.ToArray()), "avatar", "avatar.svg");

        var response = await _client.PutAsync("/api/profiles/me/avatar", content);

        await TestWebApplicationFactory.AssertValidationProblemAsync(
            response,
            "avatar",
            "Avatar upload must be a still JPEG or PNG image.");
    }

    [Fact]
    public async Task PublicProfileReadsRemainAvailable_AndAvatarEndpointServesJpeg()
    {
        await _factory.SignInAsBobAsync(_client);

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
        Assert.Null(publicAvatarResponse.Content.Headers.ContentDisposition);
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

    private async Task<HttpResponseMessage> UploadAvatarAsync(byte[] avatarBytes, string contentType = "image/png", string fileName = "avatar.png")
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(avatarBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "avatar", fileName);

        return await _client.PutAsync("/api/profiles/me/avatar", content);
    }

    private static async Task<byte[]> CreatePngAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(29, 155, 240, 255));
        await using var stream = new MemoryStream();
        await image.SaveAsync(stream, new PngEncoder());
        return stream.ToArray();
    }
}
