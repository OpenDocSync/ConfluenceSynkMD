using ConfluenceSynkMD.Configuration;
using ConfluenceSynkMD.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ConfluenceSynkMD.Tests.Services;

public sealed class ImageOptimizerTests : IDisposable
{
    private readonly List<string> _createdPaths = [];

    [Fact]
    public async Task OptimizeImageAsync_Should_ReturnInputPath_When_OptimizationIsDisabled()
    {
        var inputPath = CreatePng(1600, 900);
        var sut = CreateSut(optimizeImages: false, maxWidth: 1200);

        var result = await sut.OptimizeImageAsync(inputPath);

        result.Should().Be(inputPath);
    }

    [Fact]
    public async Task OptimizeImageAsync_Should_ReturnInputPath_When_FileDoesNotExist()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.png");
        var sut = CreateSut(optimizeImages: true, maxWidth: 1200);

        var result = await sut.OptimizeImageAsync(missingPath);

        result.Should().Be(missingPath);
    }

    [Fact]
    public async Task OptimizeImageAsync_Should_ReturnInputPath_When_ImageWidthWithinLimit()
    {
        var inputPath = CreatePng(800, 400);
        var sut = CreateSut(optimizeImages: true, maxWidth: 1280);

        var result = await sut.OptimizeImageAsync(inputPath);

        result.Should().Be(inputPath);
    }

    [Fact]
    public async Task OptimizeImageAsync_Should_CreateResizedImage_When_ImageExceedsMaxWidth()
    {
        var inputPath = CreatePng(2000, 1000);
        var sut = CreateSut(optimizeImages: true, maxWidth: 1000);

        var result = await sut.OptimizeImageAsync(inputPath);

        result.Should().NotBe(inputPath);
        File.Exists(result).Should().BeTrue();
        TrackForCleanup(result);

        using var optimized = await Image.LoadAsync(result);
        optimized.Width.Should().Be(1000);
        optimized.Height.Should().Be(500);
    }

    [Fact]
    public async Task OptimizeImageAsync_Should_ReturnInputPath_When_ImageLoadFails()
    {
        var invalidPath = CreateTextFile("not-an-image");
        var sut = CreateSut(optimizeImages: true, maxWidth: 1000);

        var result = await sut.OptimizeImageAsync(invalidPath);

        result.Should().Be(invalidPath);
    }

    public void Dispose()
    {
        foreach (var path in _createdPaths)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static ImageOptimizer CreateSut(bool optimizeImages, int maxWidth)
    {
        var settings = Options.Create(new ConfluenceSettings
        {
            OptimizeImages = optimizeImages,
            MaxImageWidth = maxWidth
        });

        var logger = Substitute.For<ILogger>();
        logger.ForContext<ImageOptimizer>().Returns(logger);

        return new ImageOptimizer(settings, logger);
    }

    private string CreatePng(int width, int height)
    {
        var path = Path.Combine(Path.GetTempPath(), $"img-{Guid.NewGuid():N}.png");
        using var image = new Image<Rgba32>(width, height);
        image.Save(path);
        TrackForCleanup(path);
        return path;
    }

    private string CreateTextFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"invalid-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, content);
        TrackForCleanup(path);
        return path;
    }

    private void TrackForCleanup(string path) => _createdPaths.Add(path);
}