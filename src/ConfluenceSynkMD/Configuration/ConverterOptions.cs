namespace ConfluenceSynkMD.Configuration;

/// <summary>
/// Converter-specific options controlling how Markdown is transformed to Confluence Storage Format.
/// Maps to Python md2conf's converter CLI arguments.
/// </summary>
public sealed record ConverterOptions
{
    /// <summary>Whether to inject anchor macros before headings for deep-linking.</summary>
    public bool HeadingAnchors { get; init; }

    /// <summary>Whether to sanitize/escape invalid URLs.</summary>
    public bool ForceValidUrl { get; init; }

    /// <summary>Whether to omit the first H1 heading (used as page title).</summary>
    public bool SkipTitleHeading { get; init; }

    /// <summary>Whether to prefer raster images over vector (SVG → PNG).</summary>
    public bool PreferRaster { get; init; }

    /// <summary>Whether to render Draw.io code blocks as image attachments.</summary>
    public bool RenderDrawio { get; init; }

    /// <summary>Whether to render Mermaid code blocks as image attachments.</summary>
    public bool RenderMermaid { get; init; } = true;

    /// <summary>Whether to render PlantUML code blocks as image attachments.</summary>
    public bool RenderPlantuml { get; init; }

    /// <summary>Whether to render LaTeX code blocks as image attachments.</summary>
    public bool RenderLatex { get; init; }

    /// <summary>Output format for rendered diagrams: "png" or "svg".</summary>
    public string DiagramOutputFormat { get; init; } = "png";

    /// <summary>Whether to render internal .md links as Confluence Web UI URLs instead of ac:link macros.</summary>
    public bool WebUiLinks { get; init; }

    /// <summary>
    /// Strategy for Confluence Web UI URL generation.
    /// Supported values: "space-title" (default), "page-id".
    /// </summary>
    public string WebUiLinkStrategy { get; init; } = "space-title";

    /// <summary>Whether to use the panel macro instead of info/note/warning macros for alerts.</summary>
    public bool UsePanel { get; init; }

    /// <summary>Whether to validate and normalize code block languages to Confluence-supported ones.</summary>
    public bool ForceValidLanguage { get; init; }

    /// <summary>Whether to show line numbers in Confluence code block macros.</summary>
    public bool CodeLineNumbers { get; init; }

    /// <summary>Whether to include source line numbers in error messages for debugging conversion failures (Python parity).</summary>
    public bool DebugLineMarkers { get; init; }

    /// <summary>Optional prefix prepended to all page titles (e.g. "[AUTO] ").</summary>
    public string? TitlePrefix { get; init; }

    /// <summary>Tool identifier injected as a generated-by marker. Set to null to disable.</summary>
    public string? GeneratedBy { get; init; } = "MARKDOWN";
}
