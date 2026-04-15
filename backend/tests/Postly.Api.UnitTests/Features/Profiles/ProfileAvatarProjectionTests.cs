using FluentAssertions;
using Postly.Api.Features.Profiles.Application;
using Postly.Api.Persistence.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Postly.Api.UnitTests.Features.Profiles;

public class ProfileAvatarProjectionTests
{
    private readonly ProfileAvatarProcessor _processor = new();

    [Fact]
    public async Task ProcessAsync_WithTransparentPng_NormalizesTo512JpegAndFlattensTransparency()
    {
        var avatarBytes = await CreateTransparentPngAsync(300, 400);

        await using var stream = new MemoryStream(avatarBytes);
        var result = await _processor.ProcessAsync(stream);

        result.IsSuccess.Should().BeTrue();
        result.AvatarBytes.Should().NotBeNull();

        await using var normalizedStream = new MemoryStream(result.AvatarBytes!);
        var format = await Image.DetectFormatAsync(normalizedStream);
        format.Should().NotBeNull();
        format!.Name.Should().Be("JPEG");

        normalizedStream.Position = 0;
        using var image = await Image.LoadAsync<Rgb24>(normalizedStream);
        image.Width.Should().Be(ProfileAvatarProcessor.NormalizedDimension);
        image.Height.Should().Be(ProfileAvatarProcessor.NormalizedDimension);
        image.Metadata.ExifProfile.Should().BeNull();

        var corner = image[0, 0];
        corner.R.Should().BeGreaterThan((byte)245);
        corner.G.Should().BeGreaterThan((byte)245);
        corner.B.Should().BeGreaterThan((byte)245);
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyUpload_ReturnsValidationFailure()
    {
        await using var stream = new MemoryStream();

        var result = await _processor.ProcessAsync(stream);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cannot be empty");
    }

    [Fact]
    public async Task ProcessAsync_WithUnsupportedFormat_ReturnsValidationFailure()
    {
        await using var stream = new MemoryStream("<svg></svg>"u8.ToArray());

        var result = await _processor.ProcessAsync(stream);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("JPEG or PNG");
    }

    [Fact]
    public async Task ProcessAsync_WithTooSmallImage_ReturnsValidationFailure()
    {
        var avatarBytes = await CreateOpaquePngAsync(128, 128);

        await using var stream = new MemoryStream(avatarBytes);
        var result = await _processor.ProcessAsync(stream);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("at least 256x256");
    }

    [Fact]
    public async Task ProcessAsync_WithOversizedDecodedImage_ReturnsValidationFailure()
    {
        var avatarBytes = await CreateOpaquePngAsync(4097, 300);

        await using var stream = new MemoryStream(avatarBytes);
        var result = await _processor.ProcessAsync(stream);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("4096");
    }

    [Fact]
    public void CreateAvatarUrl_WithCustomAvatar_ReturnsVersionedUrl()
    {
        var user = new UserAccount
        {
            Username = "bob",
            NormalizedUsername = "BOB",
            DisplayName = "Bob",
            PasswordHash = "hash",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            AvatarBytes = [1, 2, 3],
            AvatarContentType = ProfileIdentityProjection.AvatarContentType,
            AvatarUpdatedAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(123456)
        };

        var avatarUrl = ProfileIdentityProjection.CreateAvatarUrl(user);

        avatarUrl.Should().Be("/api/profiles/bob/avatar?v=123456");
    }

    [Fact]
    public void CreateAvatarUrl_WithoutCustomAvatar_ReturnsNull()
    {
        var user = new UserAccount
        {
            Username = "bob",
            NormalizedUsername = "BOB",
            DisplayName = "Bob",
            PasswordHash = "hash",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        ProfileIdentityProjection.HasCustomAvatar(user).Should().BeFalse();
        ProfileIdentityProjection.CreateAvatarUrl(user).Should().BeNull();
    }

    private static async Task<byte[]> CreateTransparentPngAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(0, 0, 0, 0));
        for (var x = 50; x < width - 50; x++)
        {
            for (var y = 50; y < height - 50; y++)
            {
                image[x, y] = new Rgba32(220, 32, 80, 255);
            }
        }

        image.Metadata.ExifProfile = new SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifProfile();

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
