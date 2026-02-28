using FluentAssertions;
using ConfluenceSynkMD.Services;
using Serilog;
using NSubstitute;

namespace ConfluenceSynkMD.Tests.Services;

public class DrawioRendererTests
{
    private static readonly SemaphoreSlim EnvLock = new(1, 1);

    [Fact]
    public void Constructor_Should_NotThrow()
    {
        var logger = Substitute.For<ILogger>();
        var act = () => new DrawioRenderer(logger);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task RenderAsync_Should_ThrowClearMessage_When_DrawioUnavailable()
    {
        var logger = Substitute.For<ILogger>();
        logger.ForContext<DrawioRenderer>().Returns(logger);
        var renderer = new DrawioRenderer(logger);

        InvalidOperationException? exception;

        await EnvLock.WaitAsync();
        try
        {
            var originalPath = Environment.GetEnvironmentVariable("PATH");
            var originalDrawio = Environment.GetEnvironmentVariable("DRAWIO_CMD");
            try
            {
                Environment.SetEnvironmentVariable("PATH", string.Empty);
                Environment.SetEnvironmentVariable("DRAWIO_CMD", @"Z:\\does-not-exist\\drawio.exe");

                exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => renderer.RenderAsync("<mxfile></mxfile>"));
            }
            finally
            {
                Environment.SetEnvironmentVariable("PATH", originalPath);
                Environment.SetEnvironmentVariable("DRAWIO_CMD", originalDrawio);
            }
        }
        finally
        {
            EnvLock.Release();
        }

        exception.Should().NotBeNull();
        exception!.Message.Should().Contain("Draw.io CLI not found");
    }

    [Fact]
    public async Task RenderFileAsync_Should_PropagateWhen_RenderingFails()
    {
        var logger = Substitute.For<ILogger>();
        logger.ForContext<DrawioRenderer>().Returns(logger);
        var renderer = new DrawioRenderer(logger);

        var file = Path.Combine(Path.GetTempPath(), $"drawio-{Guid.NewGuid():N}.drawio");
        await File.WriteAllTextAsync(file, "<mxfile></mxfile>");

        try
        {
            Func<Task> act = async () => await renderer.RenderFileAsync(file);
            await act.Should().ThrowAsync<Exception>();
        }
        finally
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
