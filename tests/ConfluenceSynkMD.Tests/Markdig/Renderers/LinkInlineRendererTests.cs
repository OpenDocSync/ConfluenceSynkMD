using FluentAssertions;
using ConfluenceSynkMD.Configuration;

namespace ConfluenceSynkMD.Tests.Markdig.Renderers;

public class LinkInlineRendererTests
{
    // ─── Images: alignment & width ──────────────────────────────────────────

    [Fact]
    public void Write_Image_WithAlignment_Should_EmitAlignAttr()
    {
        var markdown = "![Alt text](img/arch.png)";
        var layout = new LayoutOptions { ImageAlignment = "center" };
        var (xhtml, _) = RendererTestHelper.Render(markdown, layoutOptions: layout);

        xhtml.Should().Contain("ac:align=\"center\"");
    }

    [Fact]
    public void Write_Image_WithMaxWidth_Should_EmitWidthParam()
    {
        var markdown = "![Alt text](img/arch.png)";
        var layout = new LayoutOptions { ImageMaxWidth = 600 };
        var (xhtml, _) = RendererTestHelper.Render(markdown, layoutOptions: layout);

        xhtml.Should().Contain("ac:width=\"600\"");
    }

    // ─── WebUI links ────────────────────────────────────────────────────────

    [Fact]
    public void Write_WebUiLinks_MdLink_Should_EmitConfluenceDisplayUrl()
    {
        var markdown = "[Setup Guide](setup.md)";
        var opts = new ConverterOptions { WebUiLinks = true };
        var (xhtml, _) = RendererTestHelper.Render(markdown, opts,
            pageTitleMapping: new Dictionary<string, string>());

        xhtml.Should().Contain("<a href=");
        xhtml.Should().NotContain("<ac:link>");
        xhtml.Should().Contain("/wiki/display/");
        xhtml.Should().NotContain(".md");
    }

    [Fact]
    public void Write_WebUiLinks_MdLinkWithFragment_Should_PreserveFragment()
    {
        var markdown = "[Install](setup.md#install)";
        var opts = new ConverterOptions { WebUiLinks = true };
        var (xhtml, _) = RendererTestHelper.Render(markdown, opts,
            pageTitleMapping: new Dictionary<string, string>());

        xhtml.Should().Contain("/wiki/display/setup#install");
        xhtml.Should().NotContain(".md");
    }

    // ─── Internal links ─────────────────────────────────────────────────────

    [Fact]
    public void Write_MdLink_Default_Should_EmitAcLinkMacro()
    {
        var markdown = "[Other Page](other.md)";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("<ac:link>");
        xhtml.Should().Contain("ri:content-title=\"other\"");
    }

    [Fact]
    public void Write_ExternalLink_Should_EmitAnchorTag()
    {
        var markdown = "[Google](https://google.com)";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("<a href=\"https://google.com\">");
    }

    [Fact]
    public void Write_PreferRaster_Should_SwapSvgToPng()
    {
        var markdown = "![Diagram](diagram.svg)";
        var opts = new ConverterOptions { PreferRaster = true };
        var (xhtml, renderer) = RendererTestHelper.Render(markdown, opts);

        renderer.ReferencedImages.Should().Contain(img => img.FileName.EndsWith(".png"));
    }

    [Fact]
    public void Write_LocalImage_Should_RegisterInReferencedImages()
    {
        var markdown = "![Photo](images/photo.png)";
        var (_, renderer) = RendererTestHelper.Render(markdown);

        renderer.ReferencedImages.Should().HaveCount(1);
        renderer.ReferencedImages[0].FileName.Should().Be("photo.png");
    }

    // ─── TKT-002: Mapping-based link resolution ─────────────────────────────

    [Fact]
    public void Write_MdLink_WithMapping_Should_UseResolvedTitle()
    {
        var markdown = "[Guide](guides/setup.md)";
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["guides/setup.md"] = "Installation Guide"
        };
        var (xhtml, _) = RendererTestHelper.Render(markdown, pageTitleMapping: mapping);

        xhtml.Should().Contain("ri:content-title=\"Installation Guide\"");
        xhtml.Should().NotContain("ri:content-title=\"setup\"");
    }

    [Fact]
    public void Write_MdLink_WithoutMapping_Should_FallbackToFilename()
    {
        var markdown = "[Ext](external/readme.md)";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("ri:content-title=\"readme\"");
    }

    // ─── TKT-003: WebUI links with Space Key ────────────────────────────────

    [Fact]
    public void Write_WebUiLinks_WithSpaceKey_Should_IncludeSpaceInUrl()
    {
        var markdown = "[Setup](setup.md)";
        var opts = new ConverterOptions { WebUiLinks = true };
        var (xhtml, _) = RendererTestHelper.Render(markdown, opts, spaceKey: "DEV");

        xhtml.Should().Contain("/wiki/display/DEV/setup");
        xhtml.Should().NotContain(".md");
    }

    [Fact]
    public void Write_WebUiLinks_WithMapping_And_SpaceKey_And_Fragment_Should_EmitFullUrl()
    {
        var markdown = "[Install](guides/setup.md#install)";
        var opts = new ConverterOptions { WebUiLinks = true };
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["guides/setup.md"] = "Installation Guide"
        };
        var (xhtml, _) = RendererTestHelper.Render(markdown, opts,
            pageTitleMapping: mapping, spaceKey: "DOCS");

        xhtml.Should().Contain("/wiki/display/DOCS/Installation%20Guide#install");
        xhtml.Should().NotContain(".md");
    }

    [Fact]
    public void Write_MdLink_WithRelativeParentTraversal_Should_ResolveUsingSourceContext()
    {
        var markdown = "[Home](../index.md)";
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["docs/index.md"] = "Docs Home"
        };

        var (xhtml, _) = RendererTestHelper.Render(
            markdown,
            pageTitleMapping: mapping,
            sourceDocumentPath: "docs/guides/setup.md");

        xhtml.Should().Contain("ri:content-title=\"Docs Home\"");
    }

    // ─── New tests: Internal link with anchor/fragment ──────────────────────

    [Fact]
    public void Write_InternalLink_WithAnchor_Should_EmitAnchorInLinkBody()
    {
        var markdown = "[Section](page.md#section-one)";
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["page.md"] = "My Page"
        };
        var (xhtml, _) = RendererTestHelper.Render(markdown, pageTitleMapping: mapping);

        xhtml.Should().Contain("ri:content-title=\"My Page\"");
        xhtml.Should().Contain("section-one");
    }

    // ─── New tests: Attachment links ────────────────────────────────────────

    [Fact]
    public void Write_AttachmentLink_Should_EmitViewFileMacro()
    {
        var markdown = "[Download](report.pdf)";
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var (xhtml, _) = RendererTestHelper.Render(markdown, pageTitleMapping: mapping);

        // Non-.md links to files → attachment link
        xhtml.Should().Contain("ri:attachment");
        xhtml.Should().Contain("report.pdf");
    }

    // ─── New tests: ForceValidUrl sanitization ──────────────────────────────

    [Fact]
    public void Write_ForceValidUrl_Should_SanitizeUrl()
    {
        // ForceValidUrl encodes invalid characters in URLs
        var markdown = "[Docs](setup%20guide.md)";
        var opts = new ConverterOptions { ForceValidUrl = true };
        var (xhtml, _) = RendererTestHelper.Render(markdown, opts);

        xhtml.Should().Contain("ri:content-title");
    }

    // ─── New tests: Legacy link rendering (no resolver) ─────────────────────

    [Fact]
    public void WriteLinkLegacy_NoResolver_MdLink_Should_EmitAcLink()
    {
        // No pageTitleMapping and no spaceKey → no resolver injected → legacy path
        var markdown = "[Page](other-page.md)";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("<ac:link>");
        xhtml.Should().Contain("ri:content-title=\"other-page\"");
    }

    [Fact]
    public void WriteLinkLegacy_NoResolver_MdLinkWithFragment_Should_EmitAnchor()
    {
        var markdown = "[Section](other.md#details)";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("<ac:link>");
        xhtml.Should().Contain("ri:content-title=\"other\"");
        xhtml.Should().Contain("details");
    }

    [Fact]
    public void WriteLinkLegacy_NoResolver_AnchorLink_Should_EmitATag()
    {
        var markdown = "[Jump](#section-id)";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("<a href=\"#section-id\">");
    }

    [Fact]
    public void WriteLinkLegacy_NoResolver_AttachmentLink_Should_EmitRiAttachment()
    {
        var markdown = "[Download](report.xlsx)";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("ri:attachment");
        xhtml.Should().Contain("report.xlsx");
    }

    // ─── New tests: External image ──────────────────────────────────────────

    [Fact]
    public void Write_ExternalImage_Should_EmitUrlMacro()
    {
        var markdown = "![Logo](https://example.com/logo.png)";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("<ac:image");
        xhtml.Should().Contain("ri:url");
        xhtml.Should().Contain("https://example.com/logo.png");
    }

    // ─── New tests: Image with no alt text ──────────────────────────────────

    [Fact]
    public void Write_Image_NoAlt_Should_StillEmitAcImage()
    {
        var markdown = "![](img/diagram.png)";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("<ac:image");
        xhtml.Should().Contain("ri:attachment");
        xhtml.Should().Contain("diagram.png");
    }

    // ─── New tests: Mailto link ─────────────────────────────────────────────

    [Fact]
    public void WriteLinkLegacy_MailtoLink_Should_EmitAnchorTag()
    {
        var markdown = "[Email us](mailto:team@example.com)";
        var (xhtml, _) = RendererTestHelper.Render(markdown);

        xhtml.Should().Contain("<a href=\"mailto:team@example.com\">");
    }

    // ─── New tests: Image with alignment and alt ────────────────────────────

    [Fact]
    public void Write_Image_WithAlignmentAndAlt_Should_EmitBothAttrs()
    {
        var markdown = "![Architecture](img/arch.png)";
        var layout = new LayoutOptions { ImageAlignment = "center", ImageMaxWidth = 800 };
        var (xhtml, _) = RendererTestHelper.Render(markdown, layoutOptions: layout);

        xhtml.Should().Contain("ac:align=\"center\"");
        xhtml.Should().Contain("ac:width=\"800\"");
        xhtml.Should().Contain("ac:alt=\"Architecture\"");
    }
}
