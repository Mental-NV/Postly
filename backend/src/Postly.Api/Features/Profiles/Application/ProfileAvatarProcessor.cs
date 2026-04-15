using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Postly.Api.Features.Profiles.Application;

public sealed class ProfileAvatarProcessor
{
    public const long MaxUploadBytes = 5L * 1024L * 1024L;
    public const int MinSourceDimension = 256;
    public const int MaxDecodedDimension = 4096;
    public const int NormalizedDimension = 512;
    public const int JpegQuality = 90;

    public async Task<ProfileAvatarProcessingResult> ProcessAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        if (!stream.CanSeek)
        {
            using var bufferedStream = new MemoryStream();
            await stream.CopyToAsync(bufferedStream, cancellationToken);
            bufferedStream.Position = 0;
            return await ProcessSeekableStreamAsync(bufferedStream, cancellationToken);
        }

        return await ProcessSeekableStreamAsync(stream, cancellationToken);
    }

    private static async Task<ProfileAvatarProcessingResult> ProcessSeekableStreamAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        if (stream.Length == 0)
        {
            return ProfileAvatarProcessingResult.Failure("Avatar upload cannot be empty.");
        }

        if (stream.Length > MaxUploadBytes)
        {
            return ProfileAvatarProcessingResult.Failure("Avatar upload must be 5 MB or smaller.");
        }

        stream.Position = 0;
        IImageFormat? format;

        try
        {
            format = await Image.DetectFormatAsync(stream, cancellationToken);
        }
        catch (UnknownImageFormatException)
        {
            return ProfileAvatarProcessingResult.Failure("Avatar upload must be a still JPEG or PNG image.");
        }

        if (format == null || (!IsJpeg(format) && !IsPng(format)))
        {
            return ProfileAvatarProcessingResult.Failure("Avatar upload must be a still JPEG or PNG image.");
        }

        stream.Position = 0;

        try
        {
            using var image = await Image.LoadAsync<Rgba32>(stream, cancellationToken);

            if (image.Frames.Count > 1)
            {
                return ProfileAvatarProcessingResult.Failure("Animated avatar uploads are not supported.");
            }

            image.Mutate(context => context.AutoOrient());

            if (image.Width < MinSourceDimension || image.Height < MinSourceDimension)
            {
                return ProfileAvatarProcessingResult.Failure("Avatar images must be at least 256x256 pixels.");
            }

            if (image.Width > MaxDecodedDimension || image.Height > MaxDecodedDimension)
            {
                return ProfileAvatarProcessingResult.Failure("Avatar images cannot exceed 4096 pixels on either side.");
            }

            image.Metadata.ExifProfile = null;
            image.Metadata.IccProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;
            image.Metadata.CicpProfile = null;

            image.Mutate(context => context.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center,
                Size = new Size(NormalizedDimension, NormalizedDimension)
            }));

            using var flattenedImage = new Image<Rgb24>(
                NormalizedDimension,
                NormalizedDimension,
                Color.White);

            flattenedImage.Mutate(context => context.DrawImage(image, new Point(0, 0), 1f));

            flattenedImage.Metadata.ExifProfile = null;
            flattenedImage.Metadata.IccProfile = null;
            flattenedImage.Metadata.IptcProfile = null;
            flattenedImage.Metadata.XmpProfile = null;
            flattenedImage.Metadata.CicpProfile = null;

            using var output = new MemoryStream();
            await flattenedImage.SaveAsJpegAsync(output, new JpegEncoder
            {
                Quality = JpegQuality
            }, cancellationToken);

            return ProfileAvatarProcessingResult.Success(output.ToArray());
        }
        catch (UnknownImageFormatException)
        {
            return ProfileAvatarProcessingResult.Failure("Avatar upload must be a still JPEG or PNG image.");
        }
        catch (ImageFormatException)
        {
            return ProfileAvatarProcessingResult.Failure("Avatar upload could not be processed.");
        }
    }

    private static bool IsJpeg(IImageFormat format)
    {
        return string.Equals(format.Name, "JPEG", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPng(IImageFormat format)
    {
        return string.Equals(format.Name, "PNG", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record ProfileAvatarProcessingResult(
    bool IsSuccess,
    byte[]? AvatarBytes,
    string? ErrorMessage)
{
    public static ProfileAvatarProcessingResult Success(byte[] avatarBytes)
    {
        return new ProfileAvatarProcessingResult(true, avatarBytes, null);
    }

    public static ProfileAvatarProcessingResult Failure(string errorMessage)
    {
        return new ProfileAvatarProcessingResult(false, null, errorMessage);
    }
}
