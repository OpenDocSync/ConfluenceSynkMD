using ConfluenceSynkMD.Configuration;
using FluentAssertions;

namespace ConfluenceSynkMD.Tests.Markdig.Renderers;

public sealed class MathRendererTests
{
    [Fact]
    public void InlineMath_Should_RenderEazyMathInlineMacro_When_RenderLatexDisabled()
    {
        const string markdown = "Inline $a < b & c$ math.";

        var (xhtml, renderer) = RendererTestHelper.Render(markdown,
            converterOptions: new ConverterOptions { RenderLatex = false });

        xhtml.Should().Contain("ac:name=\"eazy-math-inline\"");
        xhtml.Should().Contain("a &lt; b &amp; c");
        renderer.LatexFormulas.Should().BeEmpty();
    }

    [Fact]
    public void InlineMath_Should_RenderAttachment_When_RenderLatexEnabled()
    {
        const string markdown = "Inline $x+y$ formula.";

        var (xhtml, renderer) = RendererTestHelper.Render(markdown,
            converterOptions: new ConverterOptions { RenderLatex = true });

        xhtml.Should().Contain("<ac:image><ri:attachment ri:filename=\"formula-");
        renderer.LatexFormulas.Should().ContainSingle();
        renderer.LatexFormulas[0].Source.Should().Be("x+y");
    }

}
