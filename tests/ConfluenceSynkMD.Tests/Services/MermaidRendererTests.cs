using ConfluenceSynkMD.Services;
using FluentAssertions;
using NSubstitute;
using Serilog;

namespace ConfluenceSynkMD.Tests.Services;

public sealed class MermaidRendererTests
{
    private static readonly SemaphoreSlim _envLock = new(1, 1);

    [Fact]
    public async Task RenderToPngAsync_WithoutMmdcOnPath_ThrowsClearErrorAndCleansTempFiles()
    {
        const string source = "graph TD\n  A --> B";
        var logger = Substitute.For<ILogger>();
        logger.ForContext<MermaidRenderer>().Returns(logger);
        var sut = new MermaidRenderer(logger);

        // Snapshot temp dir contents BEFORE the call so we can assert that the
        // failed render leaves no new files behind. The renderer chooses the
        // exact filename internally; the test only cares that nothing leaks.
        var tempDir = Path.Combine(Path.GetTempPath(), "ConfluenceSynkMD-mermaid");
        Directory.CreateDirectory(tempDir);
        var beforeFiles = new HashSet<string>(
            Directory.GetFiles(tempDir),
            StringComparer.OrdinalIgnoreCase);

        InvalidOperationException? exception;

        await _envLock.WaitAsync();
        try
        {
            var originalPath = Environment.GetEnvironmentVariable("PATH");
            try
            {
                Environment.SetEnvironmentVariable("PATH", string.Empty);
                exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RenderToPngAsync(source));
            }
            finally
            {
                Environment.SetEnvironmentVariable("PATH", originalPath);
            }
        }
        finally
        {
            _envLock.Release();
        }

        exception.Should().NotBeNull();
        exception!.Message.Should().Contain("mmdc is not available on PATH");

        var afterFiles = Directory.GetFiles(tempDir);
        var leaked = afterFiles.Where(f => !beforeFiles.Contains(f)).ToList();
        leaked.Should().BeEmpty("the failed render must clean up its own temp files");
    }
}
