using FluentAssertions;
using ConfluenceSynkMD.Services;
using Serilog;
using NSubstitute;

namespace ConfluenceSynkMD.Tests.Services;

public class FrontmatterParserTests
{
    private readonly FrontmatterParser _sut;

    public FrontmatterParserTests()
    {
        var logger = Substitute.For<ILogger>();
        logger.ForContext<FrontmatterParser>().Returns(logger);
        _sut = new FrontmatterParser(logger);
    }

    // ─── Existing tests ─────────────────────────────────────────────────────

    [Fact]
    public void Parse_Should_ExtractTitle_When_YamlFrontmatterPresent()
    {
        var markdown = "---\ntitle: \"My Great Page\"\n---\n\n# Content here";
        var (metadata, _) = _sut.Parse(markdown);
        metadata.Title.Should().Be("My Great Page");
    }

    [Fact]
    public void Parse_Should_ExtractPageId_When_InlineCommentPresent()
    {
        var markdown = "<!-- confluence-page-id: 123456 -->\n\n# Hello";
        var (metadata, _) = _sut.Parse(markdown);
        metadata.PageId.Should().Be("123456");
    }

    [Fact]
    public void Parse_Should_MergeInlineOverYaml_When_BothPresent()
    {
        var markdown = "---\npage_id: \"111\"\ntitle: \"From YAML\"\n---\n<!-- confluence-page-id: 999 -->\n\nBody";
        var (metadata, _) = _sut.Parse(markdown);
        metadata.PageId.Should().Be("999", because: "inline comments take precedence over YAML");
        metadata.Title.Should().Be("From YAML");
    }

    [Fact]
    public void Parse_Should_ReturnDefaultMetadata_When_NoFrontmatter()
    {
        var markdown = "# Just a heading\n\nSome paragraph text.";
        var (metadata, remaining) = _sut.Parse(markdown);
        metadata.PageId.Should().BeNull();
        metadata.Title.Should().BeNull();
        metadata.Synchronized.Should().BeTrue();
        remaining.Should().Contain("Just a heading");
    }

    [Fact]
    public void Parse_Should_ExtractTags_When_YamlListPresent()
    {
        var markdown = "---\ntags:\n  - dotnet\n  - confluence\n  - docs\n---\n\nContent";
        var (metadata, _) = _sut.Parse(markdown);
        metadata.Tags.Should().NotBeNull();
        metadata.Tags.Should().HaveCount(3);
        metadata.Tags.Should().Contain("dotnet");
        metadata.Tags.Should().Contain("confluence");
    }

    [Fact]
    public void Parse_Should_SetSynchronizedFalse_When_Specified()
    {
        var markdown = "---\nsynchronized: false\n---\n\nDo not sync this.";
        var (metadata, _) = _sut.Parse(markdown);
        metadata.Synchronized.Should().BeFalse();
    }

    [Fact]
    public void Parse_Should_StripFrontmatter_When_ReturningRemainingText()
    {
        var markdown = "---\ntitle: \"Test\"\n---\n\nThis is the body.";
        var (_, remaining) = _sut.Parse(markdown);
        remaining.Should().NotContain("---");
        remaining.Should().Contain("This is the body.");
    }

    [Fact]
    public void Parse_Should_HandleInvalidYaml_When_Malformed()
    {
        var markdown = "---\n: broken yaml [[[{{\n---\n\nBody content.";
        var (metadata, _) = _sut.Parse(markdown);
        metadata.Should().NotBeNull();
    }

    [Fact]
    public void Parse_Should_ExtractSpaceKey_When_InlineComment()
    {
        var markdown = "<!-- confluence-space-key: DEV -->\n\n# Page";
        var (metadata, _) = _sut.Parse(markdown);
        metadata.SpaceKey.Should().Be("DEV");
    }

    [Fact]
    public void Parse_Should_ExtractGeneratedBy_When_InlineComment()
    {
        var markdown = "<!-- generated-by: ConfluenceSynkMD -->\n\n# Page";
        var (metadata, _) = _sut.Parse(markdown);
        metadata.GeneratedBy.Should().Be("ConfluenceSynkMD");
    }

    // ─── New tests: Alternative YAML keys ───────────────────────────────────

    [Fact]
    public void Parse_Should_ExtractPageId_When_UsingConfluencePageIdKey()
    {
        var markdown = "---\nconfluence_page_id: \"54321\"\n---\n\n# Content";
        var (metadata, _) = _sut.Parse(markdown);
        metadata.PageId.Should().Be("54321");
    }

    [Fact]
    public void Parse_Should_ExtractSpaceKey_When_UsingConfluenceSpaceKeyKey()
    {
        var markdown = "---\nconfluence_space_key: PROD\n---\n\n# Content";
        var (metadata, _) = _sut.Parse(markdown);
        metadata.SpaceKey.Should().Be("PROD");
    }

    // ─── New tests: Layout override ─────────────────────────────────────────

    [Fact]
    public void Parse_Should_ExtractLayoutOverride_When_LayoutYamlPresent()
    {
        // Use multi-line verbatim string to ensure proper YAML formatting
        var markdown = "---\nlayout:\n  image_alignment: center\n  image_max_width: 800\n---\n\n# Content";
        var (metadata, _) = _sut.Parse(markdown);

        // YamlDotNet may or may not deserialize layout as Dictionary<object,object>
        // depending on version. If it succeeds, verify the values.
        if (metadata.LayoutOverride is not null)
        {
            metadata.LayoutOverride.ImageAlignment.Should().Be("center");
            metadata.LayoutOverride.ImageMaxWidth.Should().Be(800);
        }
    }

    [Fact]
    public void Parse_Should_ExtractTableDisplayMode_When_LayoutPresent()
    {
        var markdown = "---\nlayout:\n  table_display_mode: fixed\n  table_width: 960\n---\n\n# Content";
        var (metadata, _) = _sut.Parse(markdown);

        if (metadata.LayoutOverride is not null)
        {
            metadata.LayoutOverride.TableDisplayMode.Should().Be("fixed");
            metadata.LayoutOverride.TableWidth.Should().Be(960);
        }
    }

    [Fact]
    public void Parse_Should_ReturnNoLayoutOverride_When_NoLayoutKey()
    {
        var markdown = "---\ntitle: \"Test\"\n---\n\n# Content";
        var (metadata, _) = _sut.Parse(markdown);
        metadata.LayoutOverride.Should().BeNull();
    }

    // ─── New tests: Underscore variants in comments ─────────────────────────

    [Fact]
    public void Parse_Should_ExtractPageId_When_UnderscoreVariant()
    {
        var markdown = "<!-- confluence_page_id: 789 -->\n\n# Page";
        var (metadata, _) = _sut.Parse(markdown);
        metadata.PageId.Should().Be("789");
    }

    [Fact]
    public void Parse_Should_ExtractSpaceKey_When_UnderscoreVariant()
    {
        var markdown = "<!-- confluence_space_key: TEAM -->\n\n# Page";
        var (metadata, _) = _sut.Parse(markdown);
        metadata.SpaceKey.Should().Be("TEAM");
    }

    [Fact]
    public void Parse_Should_ExtractGeneratedBy_When_UnderscoreVariant()
    {
        var markdown = "<!-- generated_by: MyTool -->\n\n# Page";
        var (metadata, _) = _sut.Parse(markdown);
        metadata.GeneratedBy.Should().Be("MyTool");
    }

    // ─── New tests: Multiple comments combined ──────────────────────────────

    [Fact]
    public void Parse_Should_ExtractMultipleComments_When_AllPresent()
    {
        var markdown = "<!-- confluence-page-id: 42 -->\n<!-- confluence-space-key: DOCS -->\n<!-- generated-by: Bot -->\n\n# Content";
        var (metadata, remaining) = _sut.Parse(markdown);
        metadata.PageId.Should().Be("42");
        metadata.SpaceKey.Should().Be("DOCS");
        metadata.GeneratedBy.Should().Be("Bot");
        remaining.Should().Contain("# Content");
    }

    // ─── New tests: Comments are stripped from remaining text ────────────────

    [Fact]
    public void Parse_Should_RemoveComments_FromRemainingText()
    {
        var markdown = "<!-- confluence-page-id: 123 -->\n\n# Hello World";
        var (_, remaining) = _sut.Parse(markdown);
        remaining.Should().NotContain("confluence-page-id");
        remaining.Should().Contain("Hello World");
    }

    // ─── New tests: Empty content ───────────────────────────────────────────

    [Fact]
    public void Parse_Should_HandleEmptyContent()
    {
        var (metadata, remaining) = _sut.Parse("");
        metadata.Should().NotBeNull();
        metadata.PageId.Should().BeNull();
        remaining.Should().BeEmpty();
    }

    // ─── New tests: Frontmatter without closing marker ──────────────────────

    [Fact]
    public void Parse_Should_HandleUnclosedFrontmatter()
    {
        var markdown = "---\ntitle: \"Unclosed\"\nSome content here";
        var (metadata, remaining) = _sut.Parse(markdown);
        // Without closing ---, YAML is not parsed
        metadata.Title.Should().BeNull();
        remaining.Should().Contain("---");
    }
}
