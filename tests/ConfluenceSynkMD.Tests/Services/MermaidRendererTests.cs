using ConfluenceSynkMD.Services;
using FluentAssertions;
using NSubstitute;
using Serilog;

namespace ConfluenceSynkMD.Tests.Services;

public sealed class MermaidRendererTests
{
    private static readonly Lock _envLock = new();

    [Fact]
    public void GenerateFileName_IsDeterministic()
    {
        const string source = "graph TD\n  A --> B";

        var first = MermaidRenderer.GenerateFileName(source);
        var second = MermaidRenderer.GenerateFileName(source);

        first.Should().Be(second);
        first.Should().MatchRegex("^mermaid-[a-f0-9]{8}\\.png$");
    }

    [Fact]
    public void GenerateFileName_DiffersForDifferentSource()
    {
        var first = MermaidRenderer.GenerateFileName("graph TD\n  A --> B");
        var second = MermaidRenderer.GenerateFileName("graph TD\n  A --> C");

        first.Should().NotBe(second);
    }

    [Fact]
    public async Task RenderToPngAsync_WithoutDockerOnPath_ThrowsClearErrorAndCleansTempFiles()
    {
        const string source = "graph TD\n  A --> B";
        var logger = Substitute.For<ILogger>();
        logger.ForContext<MermaidRenderer>().Returns(logger);
        var sut = new MermaidRenderer(logger);

        var outputFileName = MermaidRenderer.GenerateFileName(source);
        var hash = outputFileName["mermaid-".Length..^".png".Length];
        var tempDir = Path.Combine(Path.GetTempPath(), "ConfluenceSynkMD-mermaid");
        var inputFile = Path.Combine(tempDir, $"{hash}.mmd");
        var outputFile = Path.Combine(tempDir, $"{hash}.png");

        if (File.Exists(inputFile)) File.Delete(inputFile);
        if (File.Exists(outputFile)) File.Delete(outputFile);

        InvalidOperationException? exception;

        lock (_envLock)
        {
            var originalPath = Environment.GetEnvironmentVariable("PATH");
            try
            {
                Environment.SetEnvironmentVariable("PATH", string.Empty);
                exception = Assert.ThrowsAsync<InvalidOperationException>(() => sut.RenderToPngAsync(source))
                    .GetAwaiter()
                    .GetResult();
            }
            finally
            {
                Environment.SetEnvironmentVariable("PATH", originalPath);
            }
        }

        exception.Should().NotBeNull();
        exception!.Message.Should().Contain("'docker' executable is not available on PATH");

        File.Exists(inputFile).Should().BeFalse();
        File.Exists(outputFile).Should().BeFalse();
    }
}
