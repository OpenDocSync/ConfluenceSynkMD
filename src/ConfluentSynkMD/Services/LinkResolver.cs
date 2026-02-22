using Serilog;

namespace ConfluentSynkMD.Services;

/// <summary>
/// Default implementation of <see cref="ILinkResolver"/>. Extracts link resolution
/// logic from LinkInlineRenderer into a testable, injectable service (TKT-005).
/// </summary>
public sealed class LinkResolver : ILinkResolver
{
    private readonly Dictionary<string, string> _pageTitleMapping;
    private readonly Dictionary<string, string> _pageIdMapping;
    private readonly string? _spaceKey;
    private readonly IConfluenceUrlBuilder _urlBuilder;
    private readonly ILogger? _logger;
    private readonly Action<string, string?, string>? _onUnresolvedLink;
    private readonly Action<string, string?>? _onWebUiPageIdFallback;

    public LinkResolver(
        Dictionary<string, string> pageTitleMapping,
        string? spaceKey,
        Dictionary<string, string>? pageIdMapping = null,
        IConfluenceUrlBuilder? urlBuilder = null,
        Action<string, string?, string>? onUnresolvedLink = null,
        Action<string, string?>? onWebUiPageIdFallback = null,
        ILogger? logger = null)
    {
        _pageTitleMapping = pageTitleMapping ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _pageIdMapping = pageIdMapping ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _spaceKey = spaceKey;
        _urlBuilder = urlBuilder ?? new ConfluenceUrlBuilder();
        _onUnresolvedLink = onUnresolvedLink;
        _onWebUiPageIdFallback = onWebUiPageIdFallback;
        _logger = logger?.ForContext<LinkResolver>();
    }

    public LinkResolution Resolve(string linkUrl, bool webUiMode, string? sourcePath = null)
    {
        if (IsExternalUrl(linkUrl))
        {
            return new LinkResolution(LinkType.External, linkUrl, linkUrl, null, IsResolved: true);
        }

        if (linkUrl.StartsWith('#'))
        {
            return new LinkResolution(LinkType.Anchor, string.Empty, linkUrl, linkUrl[1..], IsResolved: true);
        }

        if (IsMdLink(linkUrl))
        {
            return ResolveMdLink(linkUrl, webUiMode, sourcePath);
        }

        var filename = Path.GetFileName(linkUrl);
        return new LinkResolution(LinkType.Attachment, filename, null, null, IsResolved: true);
    }

    private LinkResolution ResolveMdLink(string url, bool webUiMode, string? sourcePath)
    {
        var parts = url.Split('#', 2);
        var pathPart = parts[0];
        var fragment = parts.Length > 1 ? parts[1] : null;

        var (title, isResolved, pageId) = ResolvePageTarget(pathPart, sourcePath);

        string? displayUrl = null;
        if (webUiMode)
        {
            displayUrl = _urlBuilder.BuildPageUrl(title, _spaceKey, fragment, pageId);

            if (_urlBuilder.Strategy == ConfluenceUrlBuilder.StrategyPageId
                && string.IsNullOrWhiteSpace(pageId))
            {
                _onWebUiPageIdFallback?.Invoke(pathPart, sourcePath);
                _logger?.Information(
                    "WebUI page-id strategy fallback for '{LinkPath}' from '{SourcePath}': no page ID mapping, using title-based URL.",
                    pathPart,
                    sourcePath ?? "<unknown>");
            }
        }

        return new LinkResolution(LinkType.InternalPage, title, displayUrl, fragment, isResolved);
    }

    private (string Title, bool IsResolved, string? PageId) ResolvePageTarget(string linkPath, string? sourcePath)
    {
        var candidates = BuildLookupCandidates(linkPath, sourcePath);
        foreach (var candidate in candidates)
        {
            if (_pageTitleMapping.TryGetValue(candidate, out var mappedTitle))
            {
                _pageIdMapping.TryGetValue(candidate, out var mappedPageId);
                return (mappedTitle, true, mappedPageId);
            }
        }

        var fallback = Path.GetFileNameWithoutExtension(linkPath.Replace('\\', '/'));
        _logger?.Warning(
            "Link target '{LinkPath}' from '{SourcePath}' not found in page mapping ({MappingCount} entries). " +
            "Falling back to filename-derived title '{FallbackTitle}'.",
            linkPath,
            sourcePath ?? "<unknown>",
            _pageTitleMapping.Count,
            fallback);
        _onUnresolvedLink?.Invoke(linkPath, sourcePath, fallback);

        return (fallback, false, null);
    }

    private static List<string> BuildLookupCandidates(string linkPath, string? sourcePath)
    {
        var normalizedLink = NormalizePath(linkPath);
        var candidates = new List<string>();

        static void AddUnique(List<string> items, string value)
        {
            if (!items.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                items.Add(value);
            }
        }

        if (!string.IsNullOrWhiteSpace(sourcePath))
        {
            var source = NormalizePath(sourcePath);
            var sourceDir = Path.GetDirectoryName(source)?.Replace('\\', '/') ?? string.Empty;
            var combined = string.IsNullOrEmpty(sourceDir)
                ? normalizedLink
                : $"{sourceDir}/{normalizedLink}";
            AddUnique(candidates, CollapsePath(combined));
        }

        AddUnique(candidates, CollapsePath(normalizedLink));
        AddUnique(candidates, CollapsePath(normalizedLink.TrimStart('.', '/')));

        return candidates;
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static string CollapsePath(string path)
    {
        var normalized = NormalizePath(path);
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var stack = new List<string>(parts.Length);

        foreach (var part in parts)
        {
            if (part == ".")
                continue;

            if (part == "..")
            {
                if (stack.Count > 0 && stack[^1] != "..")
                {
                    stack.RemoveAt(stack.Count - 1);
                }
                else
                {
                    stack.Add(part);
                }

                continue;
            }

            stack.Add(part);
        }

        return string.Join('/', stack);
    }

    private static bool IsExternalUrl(string url) =>
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);

    private static bool IsMdLink(string url) =>
        url.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
        || url.Contains(".md#", StringComparison.OrdinalIgnoreCase);
}
