using ConfluenceSynkMD.Markdig;
using FluentAssertions;

namespace ConfluenceSynkMD.Tests.Markdig;

public sealed class ConfluenceRendererTests
{
    [Fact]
    public void Constructor_Should_InitializeExpectedDefaultState()
    {
        using var writer = new StringWriter();

        var sut = new ConfluenceRenderer(writer);

        sut.ReferencedImages.Should().BeEmpty();
        sut.MermaidDiagrams.Should().BeEmpty();
        sut.DrawioDiagrams.Should().BeEmpty();
        sut.PlantUmlDiagrams.Should().BeEmpty();
        sut.LatexFormulas.Should().BeEmpty();
        sut.ConverterOptions.Should().NotBeNull();
        sut.LayoutOptions.Should().NotBeNull();
        sut.LinkResolver.Should().BeNull();
        sut.CurrentDocumentPath.Should().BeNull();
        sut.FirstHeadingSeen.Should().BeFalse();
        sut.SkipUntilEnd.Should().BeFalse();
    }

    [Fact]
    public void Constructor_Should_RegisterCoreRenderers()
    {
        using var writer = new StringWriter();
        var sut = new ConfluenceRenderer(writer);

        sut.ObjectRenderers.Should().Contain(r => r.GetType().Name == "HeadingRenderer");
        sut.ObjectRenderers.Should().Contain(r => r.GetType().Name == "CodeBlockRenderer");
        sut.ObjectRenderers.Should().Contain(r => r.GetType().Name == "LinkInlineRenderer");
        sut.ObjectRenderers.Should().Contain(r => r.GetType().Name == "MathInlineRenderer");
        sut.ObjectRenderers.Should().Contain(r => r.GetType().Name == "MathBlockRenderer");
    }
}