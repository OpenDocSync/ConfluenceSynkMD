using ConfluenceSynkMD.Configuration;
using ConfluenceSynkMD.Models;

namespace ConfluenceSynkMD.ETL.Core;

/// <summary>
/// Central context object shared across all ETL pipeline steps.
/// Each phase (Extract, Transform, Load) reads from and writes to this context,
/// eliminating the need for direct inter-step communication.
/// </summary>
public sealed class TranslationBatchContext
{
    // ─── Input Configuration ────────────────────────────────────────────────

    /// <summary>CLI-provided synchronization options.</summary>
    public required SyncOptions Options { get; init; }

    /// <summary>Converter-specific options (anchors, diagrams, line numbers, etc.).</summary>
    public ConverterOptions ConverterOptions { get; init; } = new();

    /// <summary>Layout options (image alignment, table width, etc.).</summary>
    public LayoutOptions LayoutOptions { get; init; } = new();

    // ─── Extract Phase Data ─────────────────────────────────────────────────

    /// <summary>Document nodes extracted from the local filesystem (Upload direction).</summary>
    public List<DocumentNode> ExtractedDocumentNodes { get; } = new();

    /// <summary>Confluence pages extracted from the API (Download direction).</summary>
    public List<ConfluencePageWithAttachments> ExtractedConfluencePages { get; } = new();

    // ─── Transform Phase Data ───────────────────────────────────────────────

    /// <summary>Documents transformed and ready for loading.</summary>
    public List<ConvertedDocument> TransformedDocuments { get; } = new();

    // ─── Load Phase Metrics ─────────────────────────────────────────────────

    /// <summary>Number of documents successfully loaded to the target.</summary>
    public int LoadedCount { get; set; }

    /// <summary>Number of documents that failed during load.</summary>
    public int FailedCount { get; set; }

    // ─── Shared State ───────────────────────────────────────────────────────

    /// <summary>
    /// Maps source file paths (Upload) or Confluence page IDs (Download) to
    /// their target identifiers, used for parent-child hierarchy tracking.
    /// </summary>
    public Dictionary<string, string> PageIdCache { get; } = new();

    /// <summary>
    /// Maps source file paths to the effective Confluence space key used during upload.
    /// Used by WriteBackStep to write back the correct per-document space key.
    /// </summary>
    public Dictionary<string, string> SpaceKeyCache { get; } = new();

    /// <summary>Resolved Confluence space metadata (set during Extract or Load).</summary>
    public ConfluenceSpace? ResolvedSpace { get; set; }

    // ─── Pipeline Diagnostics ───────────────────────────────────────────────

    /// <summary>Accumulated results from each pipeline step, in execution order.</summary>
    public List<PipelineResult> StepResults { get; } = new();

    /// <summary>Count of unresolved internal markdown links that required filename fallback.</summary>
    public int UnresolvedLinkFallbackCount { get; set; }

    /// <summary>Count of WebUI page-id strategy fallbacks to title-based URL generation.</summary>
    public int WebUiPageIdFallbackCount { get; set; }

    /// <summary>Sample unresolved links for diagnostics (source + link + fallback title).</summary>
    public List<string> UnresolvedLinkSamples { get; } = new();

    /// <summary>Sample page-id strategy fallbacks for diagnostics (source + link).</summary>
    public List<string> WebUiPageIdFallbackSamples { get; } = new();
}
