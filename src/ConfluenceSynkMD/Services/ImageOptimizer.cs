using ConfluenceSynkMD.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ConfluenceSynkMD.Services;

/// <summary>
/// Optimizes images for web use by resizing them if they exceed a maximum width.
/// </summary>
public sealed class ImageOptimizer : IImageOptimizer
{
    private readonly ConfluenceSettings _settings;
    private readonly ILogger _logger;
    private readonly string _tempDir;

    public ImageOptimizer(IOptions<ConfluenceSettings> settings, ILogger logger)
    {
        _settings = settings.Value;
        _logger = logger.ForContext<ImageOptimizer>();
        _tempDir = Path.Combine(Path.GetTempPath(), "ConfluenceSynkMD-optimized");
        Directory.CreateDirectory(_tempDir);
    }

    /// <summary>
    /// Checks if an image should be optimized. If so, resizes it and returns the path to the optimized version.
    /// Otherwise returns the original path.
    /// </summary>
    public async Task<string> OptimizeImageAsync(string inputPath, CancellationToken ct = default)
    {
        if (!_settings.OptimizeImages)
        {
            return inputPath;
        }

        if (!File.Exists(inputPath))
        {
            _logger.Warning("Cannot optimize non-existent image: {Path}", inputPath);
            return inputPath;
        }

        try
        {
            using var image = await Image.LoadAsync(inputPath, ct);

            if (image.Width <= _settings.MaxImageWidth)
            {
                _logger.Debug("Image '{Path}' ({Width}px) is within limits. Skipping optimization.",
                    Path.GetFileName(inputPath), image.Width);
                return inputPath;
            }

            var ratio = (double)_settings.MaxImageWidth / image.Width;
            var newHeight = (int)(image.Height * ratio);

            _logger.Information("Optimizing image '{FileName}': {Width}x{Height} -> {NewWidth}x{NewHeight}",
                Path.GetFileName(inputPath), image.Width, image.Height, _settings.MaxImageWidth, newHeight);

            image.Mutate(x => x.Resize(_settings.MaxImageWidth, newHeight));

            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(inputPath)}";
            var outputPath = Path.Combine(_tempDir, fileName);

            await image.SaveAsync(outputPath, ct);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to optimize image: {Path}", inputPath);
            return inputPath;
        }
    }
}
