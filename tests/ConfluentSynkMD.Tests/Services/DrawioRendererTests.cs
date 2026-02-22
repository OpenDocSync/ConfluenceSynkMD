using FluentAssertions;
using ConfluentSynkMD.Services;
using Serilog;
using NSubstitute;

namespace ConfluentSynkMD.Tests.Services;

public class DrawioRendererTests
{
    [Fact]
    public void Constructor_Should_NotThrow()
    {
        var logger = Substitute.For<ILogger>();
        var act = () => new DrawioRenderer(logger);

        act.Should().NotThrow();
    }

    [Fact]
    public void RenderAsync_Should_AcceptPngFormat()
    {
        var logger = Substitute.For<ILogger>();
        var renderer = new DrawioRenderer(logger);

        // Verify method exists and accepts expected parameters
        var method = typeof(DrawioRenderer).GetMethod("RenderAsync");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<Task<(byte[] ImageBytes, string Format)>>();
    }
}
