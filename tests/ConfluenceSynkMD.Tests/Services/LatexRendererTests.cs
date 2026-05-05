using ConfluenceSynkMD.Services;
using FluentAssertions;
using NSubstitute;
using Serilog;

namespace ConfluenceSynkMD.Tests.Services;

public class LatexRendererTests
{
    [Fact]
    public void Constructor_Should_NotThrow()
    {
        var logger = Substitute.For<ILogger>();
        var act = () => new LatexRenderer(logger);

        act.Should().NotThrow();
    }
}
