using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ConfluenceSynkMD.Configuration;

namespace ConfluenceSynkMD.Markdig;

/// <summary>
/// Custom Markdig renderer that produces Confluence Storage Format (XHTML)
/// instead of standard HTML. This is the C# equivalent of md2conf's
/// ConfluenceStorageFormatConverter from converter.py.
///
/// Uses Confluence-specific XML namespaces:
/// - ac: (Atlassian Confluence macros)
/// - ri: (Resource Identifiers)
/// </summary>
public sealed class ConfluenceRenderer : TextRendererBase<ConfluenceRenderer>
{
    /// <summary>
    /// Tracks local image references found during rendering, so they can
    /// be uploaded as attachments.
    /// </summary>
    public List<(string FileName, string RelativePath)> ReferencedImages { get; } = [];

    /// <summary>
    /// Tracks Mermaid diagrams found during rendering, so they can be
    /// rendered to PNG and uploaded as attachments.
    /// </summary>
    public List<(string FileName, string MermaidSource)> MermaidDiagrams { get; } = [];

    /// <summary>Tracks Draw.io diagrams found during rendering.</summary>
    public List<(string FileName, string Source)> DrawioDiagrams { get; } = [];

    /// <summary>Tracks PlantUML diagrams found during rendering.</summary>
    public List<(string FileName, string Source)> PlantUmlDiagrams { get; } = [];

    /// <summary>Tracks LaTeX formulas found during rendering.</summary>
    public List<(string FileName, string Source)> LatexFormulas { get; } = [];

    /// <summary>Converter options controlling rendering behavior.</summary>
    public ConverterOptions ConverterOptions { get; set; } = new();

    /// <summary>Layout options controlling visual appearance.</summary>
    public LayoutOptions LayoutOptions { get; set; } = new();

    /// <summary>Link resolver service for internal link resolution and WebUI URL construction (TKT-005).</summary>
    public Services.ILinkResolver? LinkResolver { get; set; }

    /// <summary>
    /// Current document path relative to sync root, used for contextual resolution of relative links.
    /// </summary>
    public string? CurrentDocumentPath { get; set; }

    /// <summary>Tracks whether the first H1 heading has been encountered (for --skip-title-heading).</summary>
    public bool FirstHeadingSeen { get; set; }

    /// <summary>
    /// When true, all content is suppressed until a <c>&lt;!-- confluence-skip-end --&gt;</c>
    /// marker is encountered. Set by <see cref="Renderers.HtmlBlockRenderer"/>.
    /// </summary>
    public bool SkipUntilEnd { get; set; }

    public ConfluenceRenderer(TextWriter writer) : base(writer)
    {
        // Block renderers
        ObjectRenderers.Add(new Renderers.HeadingRenderer());
        ObjectRenderers.Add(new Renderers.CodeBlockRenderer());
        ObjectRenderers.Add(new Renderers.ParagraphRenderer());
        ObjectRenderers.Add(new Renderers.ListRenderer());
        ObjectRenderers.Add(new Renderers.QuoteBlockRenderer());
        ObjectRenderers.Add(new Renderers.ThematicBreakRenderer());
        ObjectRenderers.Add(new Renderers.TableRenderer());
        ObjectRenderers.Add(new Renderers.HtmlBlockRenderer());

        // Inline renderers
        ObjectRenderers.Add(new Renderers.LiteralInlineRenderer());
        ObjectRenderers.Add(new Renderers.EmphasisInlineRenderer());
        ObjectRenderers.Add(new Renderers.CodeInlineRenderer());
        ObjectRenderers.Add(new Renderers.LinkInlineRenderer());
        ObjectRenderers.Add(new Renderers.LineBreakInlineRenderer());
        ObjectRenderers.Add(new Renderers.HtmlInlineRenderer());
        ObjectRenderers.Add(new Renderers.HtmlEntityInlineRenderer());
        ObjectRenderers.Add(new Renderers.AutolinkInlineRenderer());

        // Footnote renderers
        ObjectRenderers.Add(new Renderers.FootnoteGroupRenderer());
        ObjectRenderers.Add(new Renderers.FootnoteLinkRenderer());

        // Emoji renderer
        ObjectRenderers.Add(new Renderers.EmojiInlineRenderer());

        // Math renderers (inline $...$ and block $$...$$)
        ObjectRenderers.Add(new Renderers.MathInlineRenderer());
        ObjectRenderers.Add(new Renderers.MathBlockRenderer());
    }
}

