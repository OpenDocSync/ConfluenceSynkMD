using FluentAssertions;
using ConfluenceSynkMD.Configuration;

namespace ConfluenceSynkMD.Tests.Configuration;

public class ConverterOptionsTests
{
    [Fact]
    public void Defaults_Should_HaveExpectedValues()
    {
        var opts = new ConverterOptions();

        opts.RenderMermaid.Should().BeTrue("Mermaid rendering is on by default");
        opts.GeneratedBy.Should().Be("MARKDOWN");
        opts.DiagramOutputFormat.Should().Be("png");

        opts.HeadingAnchors.Should().BeFalse();
        opts.ForceValidUrl.Should().BeFalse();
        opts.SkipTitleHeading.Should().BeFalse();
        opts.PreferRaster.Should().BeFalse();
        opts.RenderDrawio.Should().BeFalse();
        opts.RenderPlantuml.Should().BeFalse();
        opts.RenderLatex.Should().BeFalse();
        opts.WebUiLinks.Should().BeFalse();
        opts.UsePanel.Should().BeFalse();
        opts.ForceValidLanguage.Should().BeFalse();
        opts.CodeLineNumbers.Should().BeFalse();
        opts.TitlePrefix.Should().BeNull();
    }

    [Fact]
    public void With_Should_CreateCorrectCopy()
    {
        var original = new ConverterOptions { HeadingAnchors = true, CodeLineNumbers = true };
        var copy = original with { ForceValidLanguage = true };

        copy.HeadingAnchors.Should().BeTrue("original value preserved");
        copy.CodeLineNumbers.Should().BeTrue("original value preserved");
        copy.ForceValidLanguage.Should().BeTrue("new value applied");
    }

    [Fact]
    public void With_Should_NotMutateOriginal()
    {
        var original = new ConverterOptions();
        _ = original with { HeadingAnchors = true };

        original.HeadingAnchors.Should().BeFalse("original must remain unchanged");
    }
}
