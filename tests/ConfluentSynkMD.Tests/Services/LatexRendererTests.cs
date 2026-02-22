using FluentAssertions;
using ConfluentSynkMD.Services;
using Serilog;
using NSubstitute;

namespace ConfluentSynkMD.Tests.Services;

public class LatexRendererTests
{
    [Fact]
    public void GenerateMathMacro_Block_Should_UseMathblock()
    {
        var result = LatexRenderer.GenerateMathMacro(@"E = mc^2", isBlock: true);

        result.Should().Contain("ac:name=\"mathblock\"");
        result.Should().Contain("E = mc^2");
    }

    [Fact]
    public void GenerateMathMacro_Inline_Should_UseMathinline()
    {
        var result = LatexRenderer.GenerateMathMacro(@"\alpha + \beta", isBlock: false);

        result.Should().Contain("ac:name=\"mathinline\"");
    }

    [Fact]
    public void GenerateMathMacro_Should_EscapeXmlEntities()
    {
        var result = LatexRenderer.GenerateMathMacro("a < b & c > d");

        result.Should().Contain("&lt;");
        result.Should().Contain("&amp;");
        result.Should().Contain("&gt;");
        result.Should().NotContain("< b");
    }

    [Fact]
    public void GenerateMathMacro_EmptyInput_Should_ProduceValidMacro()
    {
        var result = LatexRenderer.GenerateMathMacro("");

        result.Should().Contain("ac:structured-macro");
        result.Should().Contain("ac:plain-text-body");
        result.Should().Contain("CDATA[");
    }

    [Fact]
    public void Constructor_Should_NotThrow()
    {
        var logger = Substitute.For<ILogger>();
        var act = () => new LatexRenderer(logger);

        act.Should().NotThrow();
    }
}
