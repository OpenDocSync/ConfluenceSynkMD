namespace ConfluenceSynkMD.Configuration;

/// <summary>
/// Layout-specific options for controlling visual appearance in Confluence.
/// Maps to Python md2conf's layout CLI arguments.
/// </summary>
public sealed record LayoutOptions
{
    /// <summary>Image alignment: "center", "left", "right", or null for default.</summary>
    public string? ImageAlignment { get; init; }

    /// <summary>Maximum width for images in pixels. Null uses Confluence default.</summary>
    public int? ImageMaxWidth { get; init; }

    /// <summary>Table width in pixels. Null uses Confluence default.</summary>
    public int? TableWidth { get; init; }

    /// <summary>Table display mode: "responsive" or "fixed".</summary>
    public string TableDisplayMode { get; init; } = "responsive";

    /// <summary>Content alignment: "center", "left", "right", or null for default.</summary>
    public string? ContentAlignment { get; init; }
}
