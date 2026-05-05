using ConfluenceSynkMD.ETL.Core;
using ConfluenceSynkMD.ETL.Transform;
using ConfluenceSynkMD.Models;
using FluentAssertions;
using NSubstitute;
using Serilog;

namespace ConfluenceSynkMD.Tests.ETL.Transform;

/// <summary>
/// Unit tests for <see cref="MarkdownTransformStep"/> (Download direction:
/// Confluence Storage Format XHTML → Markdown).
/// </summary>
public sealed class MarkdownTransformStepTests
{
    // ─── Shared helpers ────────────────────────────────────────────────────

    private readonly ILogger _logger = Substitute.For<ILogger>();

    private MarkdownTransformStep CreateStep()
    {
        _logger.ForContext<MarkdownTransformStep>().Returns(_logger);
        return new MarkdownTransformStep(_logger);
    }

    private static TranslationBatchContext CreateContext() =>
        new()
        {
            Options = new SyncOptions(SyncMode.Download, ".", "SPACE"),
        };

    private static ConfluencePageWithAttachments MakePage(
        string xhtml,
        string title = "Test Page",
        string pageId = "123",
        string spaceId = "SP1",
        IReadOnlyList<ConfluenceAttachment>? attachments = null) =>
        new(
            Page: new ConfluencePage(
                Id: pageId,
                Title: title,
                SpaceId: spaceId,
                ParentId: null,
                Body: new ConfluencePageBody(
                    Storage: new ConfluenceStorage("storage", xhtml)),
                Version: new ConfluenceVersion(1)),
            Attachments: attachments ?? []);

    // ─── ExecuteAsync paths ────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_SinglePage_ReturnsSuccess()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        ctx.ExtractedConfluencePages.Add(MakePage("<p>Hello World</p>"));

        var result = await step.ExecuteAsync(ctx);

        result.Status.Should().Be(PipelineResultStatus.Success);
        result.ItemsProcessed.Should().Be(1);
        ctx.TransformedDocuments.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyInput_ReturnsSuccess_ZeroItems()
    {
        var step = CreateStep();
        var ctx = CreateContext();

        var result = await step.ExecuteAsync(ctx);

        result.Status.Should().Be(PipelineResultStatus.Success);
        result.ItemsProcessed.Should().Be(0);
    }

    // ─── Frontmatter generation ────────────────────────────────────────────

    [Fact]
    public async Task TransformSingle_GeneratesFrontmatter()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        ctx.ExtractedConfluencePages.Add(MakePage("<p>content</p>", title: "My Page", pageId: "42", spaceId: "DEV"));

        await step.ExecuteAsync(ctx);

        var md = ctx.TransformedDocuments[0].Content;
        md.Should().Contain("---");
        md.Should().Contain("title: \"My Page\"");
        md.Should().Contain("page_id: \"42\"");
        md.Should().Contain("space_id: \"DEV\"");
    }

    // ─── Heading conversion ────────────────────────────────────────────────

    [Theory]
    [InlineData("<h1>Title</h1>", "# Title")]
    [InlineData("<h2>Sub</h2>", "## Sub")]
    [InlineData("<h3>Deeper</h3>", "### Deeper")]
    [InlineData("<h4>H4</h4>", "#### H4")]
    [InlineData("<h5>H5</h5>", "##### H5")]
    [InlineData("<h6>H6</h6>", "###### H6")]
    public async Task ConvertElement_Headings(string xhtml, string expectedPrefix)
    {
        var step = CreateStep();
        var ctx = CreateContext();
        ctx.ExtractedConfluencePages.Add(MakePage(xhtml));

        await step.ExecuteAsync(ctx);

        ctx.TransformedDocuments[0].Content.Should().Contain(expectedPrefix);
    }

    // ─── Paragraph ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ConvertElement_Paragraph()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        ctx.ExtractedConfluencePages.Add(MakePage("<p>Some text here</p>"));

        await step.ExecuteAsync(ctx);

        ctx.TransformedDocuments[0].Content.Should().Contain("Some text here");
    }

    // ─── Inline formatting ─────────────────────────────────────────────────

    [Theory]
    [InlineData("<strong>bold</strong>", "**bold**")]
    [InlineData("<em>italic</em>", "*italic*")]
    [InlineData("<del>strikethrough</del>", "~~strikethrough~~")]
    [InlineData("<code>inline</code>", "`inline`")]
    public async Task ConvertElement_InlineFormatting(string xhtml, string expected)
    {
        var step = CreateStep();
        var ctx = CreateContext();
        ctx.ExtractedConfluencePages.Add(MakePage($"<p>{xhtml}</p>"));

        await step.ExecuteAsync(ctx);

        ctx.TransformedDocuments[0].Content.Should().Contain(expected);
    }

    // ─── Links ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConvertElement_AnchorLink()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        ctx.ExtractedConfluencePages.Add(MakePage("<a href=\"https://example.com\">Example</a>"));

        await step.ExecuteAsync(ctx);

        ctx.TransformedDocuments[0].Content.Should().Contain("[Example](https://example.com)");
    }

    // ─── Confluence macro: code ─────────────────────────────────────────────

    [Fact]
    public async Task ConvertMacro_CodeBlock()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        var xhtml = """
            <ac:structured-macro ac:name="code">
                <ac:parameter ac:name="language">python</ac:parameter>
                <ac:plain-text-body><![CDATA[print("hello")]]></ac:plain-text-body>
            </ac:structured-macro>
            """;
        ctx.ExtractedConfluencePages.Add(MakePage(xhtml));

        await step.ExecuteAsync(ctx);

        var md = ctx.TransformedDocuments[0].Content;
        md.Should().Contain("```python");
        md.Should().Contain("print(\"hello\")");
        md.Should().Contain("```");
    }

    // ─── Confluence macros: alerts ──────────────────────────────────────────

    [Theory]
    [InlineData("info", "[!NOTE]")]
    [InlineData("tip", "[!TIP]")]
    [InlineData("note", "[!IMPORTANT]")]
    [InlineData("warning", "[!WARNING]")]
    public async Task ConvertMacro_AlertTypes(string macroName, string expectedAlert)
    {
        var step = CreateStep();
        var ctx = CreateContext();
        var xhtml = $"""
            <ac:structured-macro ac:name="{macroName}">
                <ac:rich-text-body><p>Alert content</p></ac:rich-text-body>
            </ac:structured-macro>
            """;
        ctx.ExtractedConfluencePages.Add(MakePage(xhtml));

        await step.ExecuteAsync(ctx);

        var md = ctx.TransformedDocuments[0].Content;
        md.Should().Contain(expectedAlert);
        md.Should().Contain("Alert content");
    }

    // ─── Confluence macros: toc and children ────────────────────────────────

    [Fact]
    public async Task ConvertMacro_Toc()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        var xhtml = "<ac:structured-macro ac:name=\"toc\"></ac:structured-macro>";
        ctx.ExtractedConfluencePages.Add(MakePage(xhtml));

        await step.ExecuteAsync(ctx);

        ctx.TransformedDocuments[0].Content.Should().Contain("[[_TOC_]]");
    }

    [Fact]
    public async Task ConvertMacro_Children()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        var xhtml = "<ac:structured-macro ac:name=\"children\"></ac:structured-macro>";
        ctx.ExtractedConfluencePages.Add(MakePage(xhtml));

        await step.ExecuteAsync(ctx);

        ctx.TransformedDocuments[0].Content.Should().Contain("[[_LISTING_]]");
    }

    // ─── Unknown macro → HTML comment ───────────────────────────────────────

    [Fact]
    public async Task ConvertMacro_Unknown_EmitsComment()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        var xhtml = "<ac:structured-macro ac:name=\"custom-unknown\"><ac:rich-text-body><p>data</p></ac:rich-text-body></ac:structured-macro>";
        ctx.ExtractedConfluencePages.Add(MakePage(xhtml));

        await step.ExecuteAsync(ctx);

        ctx.TransformedDocuments[0].Content.Should().Contain("<!-- confluence-macro: custom-unknown");
    }

    // ─── Lists ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConvertList_Unordered()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        ctx.ExtractedConfluencePages.Add(MakePage("<ul><li>Alpha</li><li>Beta</li></ul>"));

        await step.ExecuteAsync(ctx);

        var md = ctx.TransformedDocuments[0].Content;
        md.Should().Contain("- Alpha");
        md.Should().Contain("- Beta");
    }

    [Fact]
    public async Task ConvertList_Ordered()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        ctx.ExtractedConfluencePages.Add(MakePage("<ol><li>First</li><li>Second</li></ol>"));

        await step.ExecuteAsync(ctx);

        var md = ctx.TransformedDocuments[0].Content;
        md.Should().Contain("1. First");
        md.Should().Contain("2. Second");
    }

    // ─── Table ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConvertTable_Simple()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        var xhtml = "<table><tr><th>Col1</th><th>Col2</th></tr><tr><td>A</td><td>B</td></tr></table>";
        ctx.ExtractedConfluencePages.Add(MakePage(xhtml));

        await step.ExecuteAsync(ctx);

        var md = ctx.TransformedDocuments[0].Content;
        md.Should().Contain("| Col1 | Col2 |");
        md.Should().Contain("| --- | --- |");
        md.Should().Contain("| A | B |");
    }

    // ─── CDATA handling ─────────────────────────────────────────────────────

    [Fact]
    public async Task CdataBlock_Preserved()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        var xhtml = """
            <ac:structured-macro ac:name="code">
                <ac:parameter ac:name="language">js</ac:parameter>
                <ac:plain-text-body><![CDATA[console.log("hello")]]></ac:plain-text-body>
            </ac:structured-macro>
            """;
        ctx.ExtractedConfluencePages.Add(MakePage(xhtml));

        await step.ExecuteAsync(ctx);

        var md = ctx.TransformedDocuments[0].Content;
        md.Should().Contain("console.log(\"hello\")");
    }

    // ─── Metadata macro extraction ──────────────────────────────────────────

    [Fact]
    public async Task TransformSingle_ExtractsMetadataFromExpandMacro()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        var xhtml = """
            <p>Content</p>
            <ac:structured-macro ac:name="expand">
                <ac:parameter ac:name="title">__ConfluenceSynkMD_metadata__</ac:parameter>
                <ac:rich-text-body><p>source-file:readme.md
            source-path:docs/readme.md</p></ac:rich-text-body>
            </ac:structured-macro>
            """;
        ctx.ExtractedConfluencePages.Add(MakePage(xhtml));

        await step.ExecuteAsync(ctx);

        var doc = ctx.TransformedDocuments[0];
        doc.OriginalFilename.Should().Be("readme.md");
        doc.OriginalSourcePath.Should().Be("docs/readme.md");
    }

    // ─── Auto-generated info macro stripping ────────────────────────────────

    [Fact]
    public async Task TransformSingle_StripsGeneratedByInfoMacro()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        var xhtml = """
            <ac:structured-macro ac:name="info" ac:schema-version="1">
                <ac:rich-text-body><p>MARKDOWN</p></ac:rich-text-body>
            </ac:structured-macro>
            <p>Actual content</p>
            """;
        ctx.ExtractedConfluencePages.Add(MakePage(xhtml));

        await step.ExecuteAsync(ctx);

        var md = ctx.TransformedDocuments[0].Content;
        md.Should().Contain("Actual content");
        md.Should().NotContain("[!NOTE]"); // info macro should be stripped, not converted to alert
    }

    // ─── ac:link → internal link ────────────────────────────────────────────

    [Fact]
    public async Task ConvertElement_AcLink_ProducesMarkdownLink()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        var xhtml = """
            <ac:link>
                <ri:page ri:content-title="Other Page" />
                <ac:plain-text-link-body><![CDATA[Link Text]]></ac:plain-text-link-body>
            </ac:link>
            """;
        ctx.ExtractedConfluencePages.Add(MakePage(xhtml));

        await step.ExecuteAsync(ctx);

        var md = ctx.TransformedDocuments[0].Content;
        md.Should().Contain("Other Page");
    }

    // ─── ac:image → markdown image ──────────────────────────────────────────

    [Fact]
    public async Task ConvertElement_AcImage_Attachment()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        var xhtml = """
            <ac:image>
                <ri:attachment ri:filename="diagram.png" />
            </ac:image>
            """;
        ctx.ExtractedConfluencePages.Add(MakePage(xhtml));

        await step.ExecuteAsync(ctx);

        var md = ctx.TransformedDocuments[0].Content;
        md.Should().Contain("diagram.png");
    }

    [Fact]
    public async Task ConvertElement_AcImage_Url()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        var xhtml = """
            <ac:image>
                <ri:url ri:value="https://example.com/logo.png" />
            </ac:image>
            """;
        ctx.ExtractedConfluencePages.Add(MakePage(xhtml));

        await step.ExecuteAsync(ctx);

        var md = ctx.TransformedDocuments[0].Content;
        md.Should().Contain("https://example.com/logo.png");
    }

    // ─── Multiple pages ─────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_MultiplePages_TransformsAll()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        ctx.ExtractedConfluencePages.Add(MakePage("<p>Page 1</p>", title: "Page 1", pageId: "1"));
        ctx.ExtractedConfluencePages.Add(MakePage("<p>Page 2</p>", title: "Page 2", pageId: "2"));

        var result = await step.ExecuteAsync(ctx);

        result.Status.Should().Be(PipelineResultStatus.Success);
        result.ItemsProcessed.Should().Be(2);
        ctx.TransformedDocuments.Should().HaveCount(2);
    }

    // ─── Attachments pass-through ───────────────────────────────────────────

    [Fact]
    public async Task TransformSingle_AttachmentInfo_IncludedInOutput()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        var attachments = new List<ConfluenceAttachment>
        {
            new("att1", "image.png", "123", "image/png", 1024,
                new ConfluenceAttachmentLinks("/wiki/download/attachments/123/image.png"))
        };
        ctx.ExtractedConfluencePages.Add(MakePage("<p>Content</p>", attachments: attachments));

        await step.ExecuteAsync(ctx);

        var doc = ctx.TransformedDocuments[0];
        doc.Attachments.Should().HaveCount(1);
        doc.Attachments[0].FileName.Should().Be("image.png");
        doc.Attachments[0].MimeType.Should().Be("image/png");
    }

    // ─── Round-trip fidelity (B3 + B4) ──────────────────────────────────────

    [Fact]
    public async Task TransformSingle_PlainBlockquote_RoundTripsAsMarkdownQuote()
    {
        // Storage Format from the upgraded upload path emits a plain <blockquote>
        // for plain Markdown quotes (no [!TYPE] alert). The download must produce
        // "> " prefixed Markdown — not flatten to a paragraph.
        var step = CreateStep();
        var ctx = CreateContext();
        ctx.ExtractedConfluencePages.Add(MakePage(
            "<blockquote><p>Just a normal quote.</p></blockquote>"));

        await step.ExecuteAsync(ctx);

        var content = ctx.TransformedDocuments[0].Content;
        content.Should().Contain("> Just a normal quote.");
        content.Should().NotContain("[!NOTE]");
    }

    [Fact]
    public async Task TransformSingle_LinkWithBoldText_PreservesInlineFormatting()
    {
        // Anchor body must recurse into children so <strong>/<em>/<code> survive
        // the round-trip. Earlier versions used .TextContent here and silently
        // dropped the inline tags.
        var step = CreateStep();
        var ctx = CreateContext();
        ctx.ExtractedConfluencePages.Add(MakePage(
            "<p><a href=\"https://example.com\"><strong>bold link</strong></a></p>"));

        await step.ExecuteAsync(ctx);

        var content = ctx.TransformedDocuments[0].Content;
        content.Should().Contain("[**bold link**](https://example.com)");
    }

    [Fact]
    public async Task TransformSingle_LinkWithMixedFormatting_PreservesAllInlineTags()
    {
        var step = CreateStep();
        var ctx = CreateContext();
        ctx.ExtractedConfluencePages.Add(MakePage(
            "<p><a href=\"/docs\">see <em>the</em> <code>docs</code></a></p>"));

        await step.ExecuteAsync(ctx);

        var content = ctx.TransformedDocuments[0].Content;
        content.Should().Contain("[see *the* `docs`](/docs)");
    }

    // ─── Round-trip fidelity follow-ups (post-/review adversarial pass) ─────

    [Fact]
    public async Task TransformSingle_CodeBlock_EndingInCdataTerminator_RoundTripsExactly()
    {
        // Bug X: the upload escape splits "]]>" across two CDATA sections
        // (placeholder0 + placeholder1). After the download resolves both placeholders
        // back to "<root>data ]]>", a naive call to StripCdataMarkers used to chop the
        // trailing "]]>" off — a silent payload truncation that defeats the upload fix.
        var step = CreateStep();
        var ctx = CreateContext();
        ctx.ExtractedConfluencePages.Add(MakePage(
            "<ac:structured-macro ac:name=\"code\">" +
            "<ac:parameter ac:name=\"language\">xml</ac:parameter>" +
            "<ac:plain-text-body><![CDATA[<root>data ]]]]><![CDATA[></root>]]></ac:plain-text-body>" +
            "</ac:structured-macro>"));

        await step.ExecuteAsync(ctx);

        var content = ctx.TransformedDocuments[0].Content;
        content.Should().Contain("<root>data ]]></root>",
            "the resolved code-block payload must be byte-equivalent, including trailing ]]>");
    }

    [Fact]
    public async Task TransformSingle_CodeBlock_PayloadContainingPlaceholderText_NotOverSubstituted()
    {
        // Bug Z: a code block whose user-typed content happens to include the literal
        // string "CDATA_PLACEHOLDER_5" must NOT be replaced with another block's
        // content. The single-pass regex resolution only substitutes placeholders
        // that are actually keys in the cdataBlocks dictionary.
        var step = CreateStep();
        var ctx = CreateContext();
        // Two code blocks: first contains user text that mentions a placeholder name
        // (will only ever be CDATA_PLACEHOLDER_0 because there's only one CDATA section
        // in this fixture), second is a normal block.
        ctx.ExtractedConfluencePages.Add(MakePage(
            "<ac:structured-macro ac:name=\"code\">" +
            "<ac:parameter ac:name=\"language\">text</ac:parameter>" +
            "<ac:plain-text-body><![CDATA[Note: do not type CDATA_PLACEHOLDER_42 in your code]]></ac:plain-text-body>" +
            "</ac:structured-macro>"));

        await step.ExecuteAsync(ctx);

        var content = ctx.TransformedDocuments[0].Content;
        content.Should().Contain("Note: do not type CDATA_PLACEHOLDER_42 in your code",
            "user-typed text matching the placeholder pattern but not in the dictionary must be preserved verbatim");
    }

    [Fact]
    public async Task TransformSingle_TableCell_WithEmbeddedNewline_StaysOnSingleRow()
    {
        // Bug Y: B1 made soft breaks emit literal "\n" in <p> content. AngleSharp
        // preserves that whitespace, so a <td> built from two source lines now contains
        // a real newline. ConvertTable used to dump the raw text between pipes, which
        // shredded the table — the row delimiter IS a newline, so an embedded newline
        // turned everything after the first soft break into prose.
        var step = CreateStep();
        var ctx = CreateContext();
        ctx.ExtractedConfluencePages.Add(MakePage(
            "<table>" +
            "<tr><th>Header A</th><th>Header B</th></tr>" +
            "<tr><td>cell with\nsoft break</td><td>plain</td></tr>" +
            "</table>"));

        await step.ExecuteAsync(ctx);

        var content = ctx.TransformedDocuments[0].Content;
        // The full row must be on a single line — find the row that mentions our text
        // and confirm the second column ("plain") is still on the same row.
        var rowLine = content.Split('\n').FirstOrDefault(l => l.Contains("cell with"));
        rowLine.Should().NotBeNull();
        rowLine!.Should().Contain("plain", "the second cell must remain on the same row as the first");
        rowLine.Should().Contain("cell with soft break", "internal whitespace collapses to a single space");
    }
}
