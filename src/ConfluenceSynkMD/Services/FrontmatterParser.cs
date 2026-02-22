using System.Text.RegularExpressions;
using ConfluenceSynkMD.Configuration;
using ConfluenceSynkMD.Models;
using Serilog;
using YamlDotNet.Serialization;

namespace ConfluenceSynkMD.Services;

/// <summary>
/// Parses YAML frontmatter and inline HTML comments from Markdown files.
/// Mirrors md2conf's scanner.py and frontmatter.py logic.
/// </summary>
public sealed partial class FrontmatterParser
{
    private readonly ILogger _logger;
    private readonly IDeserializer _yaml;

    public FrontmatterParser(ILogger logger)
    {
        _logger = logger.ForContext<FrontmatterParser>();
        _yaml = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Extracts metadata from a Markdown file's content.
    /// Returns the metadata and the remaining Markdown text.
    /// </summary>
    public (DocumentMetadata Metadata, string RemainingText) Parse(string markdownContent)
    {
        var text = markdownContent;

        // 1. Extract inline HTML comments (analogous to scanner.py)
        var pageId = ExtractComment(PageIdPattern(), ref text);
        var spaceKey = ExtractComment(SpaceKeyPattern(), ref text);
        var generatedBy = ExtractComment(GeneratedByPattern(), ref text);

        // 2. Extract YAML frontmatter block (analogous to frontmatter.py)
        var (yamlMeta, remainingText) = ExtractYamlFrontmatter(text);

        // 3. Merge: inline comments take precedence over frontmatter
        var metadata = new DocumentMetadata(
            PageId: pageId ?? yamlMeta?.PageId,
            SpaceKey: spaceKey ?? yamlMeta?.SpaceKey,
            Title: yamlMeta?.Title,
            Tags: yamlMeta?.Tags,
            Synchronized: yamlMeta?.Synchronized ?? true,
            GeneratedBy: generatedBy ?? yamlMeta?.GeneratedBy,
            Properties: yamlMeta?.Properties);

        return (metadata, remainingText);
    }

    private (DocumentMetadata? Metadata, string RemainingText) ExtractYamlFrontmatter(string text)
    {
        if (!text.StartsWith("---", StringComparison.Ordinal))
            return (null, text);

        var endIndex = text.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (endIndex < 0)
            return (null, text);

        var yamlBlock = text[(3)..endIndex].Trim();
        var remaining = text[(endIndex + 4)..].TrimStart('\r', '\n');

        try
        {
            var dict = _yaml.Deserialize<Dictionary<string, object>>(yamlBlock);
            if (dict is null)
                return (null, remaining);

            var layout = TryGetLayout(dict);

            var meta = new DocumentMetadata(
                PageId: TryGetString(dict, "page_id") ?? TryGetString(dict, "confluence_page_id"),
                SpaceKey: TryGetString(dict, "space_key") ?? TryGetString(dict, "confluence_space_key"),
                Title: TryGetString(dict, "title"),
                Tags: TryGetStringList(dict, "tags"),
                Synchronized: TryGetBool(dict, "synchronized") ?? true,
                GeneratedBy: TryGetString(dict, "generated_by"),
                LayoutOverride: layout);

            return (meta, remaining);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to parse YAML frontmatter, treating as regular content.");
            return (null, text);
        }
    }

    private static string? ExtractComment(Regex pattern, ref string text)
    {
        var match = pattern.Match(text);
        if (!match.Success) return null;

        text = text[..match.Index] + text[(match.Index + match.Length)..];
        return match.Groups[1].Value.Trim();
    }

    private static string? TryGetString(Dictionary<string, object> dict, string key) =>
        dict.TryGetValue(key, out var value) ? value?.ToString() : null;

    private static bool? TryGetBool(Dictionary<string, object> dict, string key) =>
        dict.TryGetValue(key, out var value) && value is bool b ? b :
        dict.TryGetValue(key, out var sv) && bool.TryParse(sv?.ToString(), out var parsed) ? parsed : null;

    private static List<string>? TryGetStringList(Dictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value)) return null;
        if (value is List<object> list)
            return list.Select(x => x.ToString()!).ToList();
        return null;
    }

    /// <summary>
    /// Parses optional layout override from frontmatter YAML.
    /// Supports nested 'layout' key with sub-keys like 'image_alignment', 'image_max_width',
    /// 'table_width', 'table_display_mode', and 'alignment'.
    /// </summary>
    private static LayoutOptions? TryGetLayout(Dictionary<string, object> dict)
    {
        if (!dict.TryGetValue("layout", out var layoutObj)) return null;
        if (layoutObj is not Dictionary<object, object> layoutDict) return null;

        // Flatten nested keys (e.g. 'image.alignment' or 'image_alignment')
        var flat = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in layoutDict)
        {
            var key = k.ToString()!;
            if (v is Dictionary<object, object> nested)
            {
                foreach (var (nk, nv) in nested)
                    flat[$"{key}_{nk}"] = nv?.ToString() ?? "";
            }
            else
            {
                flat[key] = v?.ToString() ?? "";
            }
        }

        int? imageMaxWidth = flat.TryGetValue("image_max_width", out var mw) && int.TryParse(mw, out var mwv) ? mwv : null;
        int? tableWidth = flat.TryGetValue("table_width", out var tw) && int.TryParse(tw, out var twv) ? twv : null;

        return new LayoutOptions
        {
            ImageAlignment = flat.GetValueOrDefault("image_alignment"),
            ImageMaxWidth = imageMaxWidth,
            TableWidth = tableWidth,
            TableDisplayMode = flat.GetValueOrDefault("table_display_mode", "responsive"),
            ContentAlignment = flat.GetValueOrDefault("alignment"),
        };
    }

    // Regex patterns matching md2conf's scanner.py extract_value calls
    [GeneratedRegex(@"<!--\s+confluence[-_]page[-_]id:\s*(\d+)\s+-->", RegexOptions.Compiled)]
    private static partial Regex PageIdPattern();

    [GeneratedRegex(@"<!--\s+confluence[-_]space[-_]key:\s*(\S+)\s+-->", RegexOptions.Compiled)]
    private static partial Regex SpaceKeyPattern();

    [GeneratedRegex(@"<!--\s+generated[-_]by:\s*(.*)\s+-->", RegexOptions.Compiled)]
    private static partial Regex GeneratedByPattern();
}
