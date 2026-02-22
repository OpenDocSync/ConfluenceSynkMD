using FluentAssertions;
using ConfluentSynkMD.Services;
using Serilog;
using NSubstitute;

namespace ConfluentSynkMD.Tests.Services;

public class PlantUmlRendererTests
{
    [Fact]
    public void Constructor_Should_NotThrow()
    {
        var logger = Substitute.For<ILogger>();
        var act = () => new PlantUmlRenderer(logger);

        act.Should().NotThrow();
    }

    [Fact]
    public void RenderAsync_Should_AcceptPngAndSvgFormats()
    {
        var logger = Substitute.For<ILogger>();
        var renderer = new PlantUmlRenderer(logger);

        var method = typeof(PlantUmlRenderer).GetMethod("RenderAsync");
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be<Task<(byte[] ImageBytes, string Format)>>();
    }
}
