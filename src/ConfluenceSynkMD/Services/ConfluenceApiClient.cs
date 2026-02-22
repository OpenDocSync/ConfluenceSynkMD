using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using ConfluenceSynkMD.Configuration;
using ConfluenceSynkMD.Models;
using Microsoft.Extensions.Options;
using Serilog;

namespace ConfluenceSynkMD.Services;

/// <summary>
/// Confluence Cloud REST API client using HttpClient.
/// Supports Basic Auth (email+token) and Bearer Token (OAuth 2.0).
/// Mirrors md2conf's ConfluenceSession from api.py.
/// </summary>
public sealed class ConfluenceApiClient : IConfluenceApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger _logger;
    private readonly ConfluenceSettings _settings;
    private readonly string _baseUrl;
    private readonly string _apiBase;
    private readonly string _v2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ConfluenceApiClient(
        HttpClient http,
        IOptions<ConfluenceSettings> settings,
        ILogger logger)
    {
        _http = http;
        _logger = logger.ForContext<ConfluenceApiClient>();
        _settings = settings.Value;
        (_baseUrl, _apiBase) = NormalizeEndpointSettings(_settings);
        _v2 = $"{_apiBase}/api/{_settings.ApiVersion}";

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _http.BaseAddress = new Uri(_baseUrl, UriKind.Absolute);

        if (_settings.AuthMode.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.BearerToken);
            _logger.Debug("Using Bearer token authentication.");
        }
        else
        {
            // Basic Auth: Base64(email:apiToken)
            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_settings.UserEmail}:{_settings.ApiToken}"));
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
            _logger.Debug("Using Basic authentication for user '{Email}'.", _settings.UserEmail);
        }

        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        // Apply custom headers from configuration
        foreach (var (key, value) in _settings.CustomHeaders)
            _http.DefaultRequestHeaders.Add(key, value);
    }

    private static (string BaseUrl, string ApiPath) NormalizeEndpointSettings(ConfluenceSettings settings)
    {
        if (!Uri.TryCreate(settings.BaseUrl.Trim(), UriKind.Absolute, out var baseUri)
            || !string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "BaseUrl must be a valid absolute HTTPS URL (e.g. https://yoursite.atlassian.net).");
        }

        var normalizedApiPath = NormalizeApiPath(settings.ApiPath);
        var basePath = baseUri.AbsolutePath.Trim('/');
        if (!string.IsNullOrEmpty(basePath))
        {
            var basePathSegment = $"/{basePath}";
            if (string.IsNullOrEmpty(normalizedApiPath))
            {
                normalizedApiPath = basePathSegment;
            }
            else if (!string.Equals(normalizedApiPath, basePathSegment, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"BaseUrl path '{basePathSegment}' conflicts with ApiPath '{normalizedApiPath}'. " +
                    "Use a host-only BaseUrl or set ApiPath to the same path.");
            }
        }

        return ($"{baseUri.Scheme}://{baseUri.Authority}", normalizedApiPath);
    }

    private static string NormalizeApiPath(string? apiPath)
    {
        var trimmed = apiPath?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed) || trimmed == "/")
            return string.Empty;

        var segment = trimmed.Trim('/');
        return string.IsNullOrEmpty(segment) ? string.Empty : $"/{segment}";
    }

    // ─── Space ──────────────────────────────────────────────────────────────

    public async Task<ConfluenceSpace> GetSpaceByKeyAsync(string spaceKey, CancellationToken ct = default)
    {
        _logger.Debug("Fetching space with key '{Key}'.", spaceKey);
        var result = await GetAsync<ConfluencePaginatedResult<ConfluenceSpace>>(
            $"{_v2}/spaces?keys={spaceKey}", ct);
        return result.Results.Count > 0 ? result.Results[0]
               : throw new InvalidOperationException($"Space '{spaceKey}' not found.");
    }

    // ─── Pages ──────────────────────────────────────────────────────────────

    public async Task<ConfluencePage?> GetPageByIdAsync(string pageId, CancellationToken ct = default)
    {
        _logger.Debug("Fetching page by ID '{Id}'.", pageId);
        try
        {
            return await GetAsync<ConfluencePage>(
                $"{_v2}/pages/{pageId}?body-format=storage", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<ConfluencePage?> GetPageByTitleAsync(
        string title, string spaceId, CancellationToken ct = default)
    {
        var matches = await GetPagesByTitleAsync(title, spaceId, ct);
        return matches.Count > 0 ? matches[0] : null;
    }

    public async IAsyncEnumerable<ConfluencePage> GetChildPagesAsync(
        string parentId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        _logger.Debug("Fetching children of page '{Id}'.", parentId);
        await foreach (var page in FetchPaginatedAsync<ConfluencePage>(
            $"{_v2}/pages/{parentId}/children?body-format=storage", ct))
        {
            yield return page;
        }
    }

    public async IAsyncEnumerable<ConfluencePage> GetPagesInSpaceAsync(
        string spaceId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        _logger.Debug("Fetching all pages in space '{SpaceId}'.", spaceId);
        await foreach (var page in FetchPaginatedAsync<ConfluencePage>(
            $"{_v2}/spaces/{spaceId}/pages?body-format=storage", ct))
        {
            yield return page;
        }
    }

    public async Task<ConfluencePage> CreatePageAsync(
        string title, string content, string parentId, string spaceId,
        CancellationToken ct = default)
    {
        _logger.Information("Creating page '{Title}' under parent '{Parent}'.", title, parentId);
        var request = new CreatePageRequest(
            SpaceId: spaceId,
            Title: title,
            ParentId: parentId,
            Body: new ConfluencePageBody(new ConfluenceStorage("storage", content)));

        return await PostAsync<CreatePageRequest, ConfluencePage>($"{_v2}/pages", request, ct);
    }

    public async Task<ConfluencePage> UpdatePageAsync(
        string pageId, string title, string content, int newVersion,
        CancellationToken ct = default)
    {
        _logger.Information("Updating page '{Id}' to version {Version}.", pageId, newVersion);
        var request = new UpdatePageRequest(
            Id: pageId,
            Title: title,
            Body: new ConfluencePageBody(new ConfluenceStorage("storage", content)),
            Version: new ConfluenceVersion(newVersion));

        return await PutAsync<UpdatePageRequest, ConfluencePage>(
            $"{_v2}/pages/{pageId}", request, ct);
    }



    public async Task<ConfluencePage> GetOrCreatePageAsync(
        string title, string parentId, string spaceId,
        CancellationToken ct = default)
    {
        var resolution = await GetOrCreatePageUnderParentAsync(title, parentId, spaceId, ct);
        if (resolution.Status == ConfluenceRootPageResolutionStatus.Ambiguous)
        {
            throw new InvalidOperationException(
                $"Ambiguous page title '{title}' in space '{spaceId}': " +
                $"{resolution.TotalMatches} matches ({resolution.MatchesUnderParent} under parent '{parentId}').");
        }

        return resolution.Page
               ?? throw new InvalidOperationException(
                   $"No page resolved for '{title}' under parent '{parentId}'.");
    }

    public async Task<ConfluenceRootPageResolution> GetOrCreatePageUnderParentAsync(
        string title, string parentId, string spaceId,
        CancellationToken ct = default)
    {
        var matches = await GetPagesByTitleAsync(title, spaceId, ct);
        var underParent = matches
            .Where(p => string.Equals(p.ParentId, parentId, StringComparison.Ordinal))
            .ToList();

        if (underParent.Count == 1)
        {
            var existing = underParent[0];
            _logger.Debug(
                "Found existing page '{Title}' (ID: {Id}) under expected parent '{ParentId}'.",
                title, existing.Id, parentId);
            return new ConfluenceRootPageResolution(
                existing,
                ConfluenceRootPageResolutionStatus.FoundUnderParent,
                matches.Count,
                underParent.Count);
        }

        var ambiguous = underParent.Count > 1
                        || (underParent.Count == 0 && matches.Count > 1);
        if (ambiguous)
        {
            _logger.Warning(
                "Ambiguous root-page resolution for '{Title}' in space '{SpaceId}': " +
                "{TotalMatches} matches, {MatchesUnderParent} under parent '{ParentId}'.",
                title, spaceId, matches.Count, underParent.Count, parentId);
            return new ConfluenceRootPageResolution(
                null,
                ConfluenceRootPageResolutionStatus.Ambiguous,
                matches.Count,
                underParent.Count);
        }

        if (matches.Count == 1)
        {
            var otherParent = matches[0].ParentId;
            _logger.Information(
                "Page '{Title}' exists once under parent '{ActualParent}', " +
                "creating a new page under '{ExpectedParent}'.",
                title, otherParent, parentId);
        }

        var created = await CreatePageAsync(title, string.Empty, parentId, spaceId, ct);
        return new ConfluenceRootPageResolution(
            created,
            ConfluenceRootPageResolutionStatus.Created,
            matches.Count,
            underParent.Count);
    }

    private async Task<IReadOnlyList<ConfluencePage>> GetPagesByTitleAsync(
        string title, string spaceId, CancellationToken ct = default)
    {
        _logger.Debug("Looking up page by title '{Title}' in space '{SpaceId}'.", title, spaceId);
        var encoded = Uri.EscapeDataString(title);
        var result = await GetAsync<ConfluencePaginatedResult<ConfluencePage>>(
            $"{_v2}/pages?space-id={spaceId}&title={encoded}&body-format=storage", ct);
        return result.Results;
    }

    // ─── Attachments (v1 API) ───────────────────────────────────────────────


    public async Task UploadAttachmentAsync(
        string pageId, string fileName, Stream content, string mimeType,
        CancellationToken ct = default)
    {
        // Check if attachment exists to determine update vs create
        ConfluenceAttachment? existing = null;
        await foreach (var att in GetAttachmentsAsync(pageId, ct))
        {
            if (att.Title == fileName)
            {
                existing = att;
                break;
            }
        }

        using var form = new MultipartFormDataContent();
        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        form.Add(streamContent, "file", fileName);
        form.Add(new StringContent("true"), "minorEdit");

        var url = existing is null
            ? $"{_apiBase}/rest/api/content/{pageId}/child/attachment"
            : $"{_apiBase}/rest/api/content/{pageId}/child/attachment/{existing.Id}/data";

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = form
        };
        request.Headers.Add("X-Atlassian-Token", "nocheck");

        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.Error("Failed to upload attachment '{File}' (ID: {Id}) to page '{PageId}'. Status: {Status}, Body: {Body}",
                fileName, existing?.Id ?? "New", pageId, response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }

        _logger.Information("{Action} attachment '{File}' (ID: {Id}) on page '{PageId}'.",
            existing is null ? "Uploaded" : "Updated", fileName, existing?.Id ?? "New", pageId);
    }

    public async IAsyncEnumerable<ConfluenceAttachment> GetAttachmentsAsync(
        string pageId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        _logger.Debug("Listing attachments on page '{Id}'.", pageId);
        await foreach (var att in FetchPaginatedAsync<ConfluenceAttachment>(
            $"{_apiBase}/rest/api/content/{pageId}/child/attachment?expand=version", ct))
        {
            yield return att;
        }
    }

    public async Task<Stream> DownloadAttachmentAsync(string downloadPath, CancellationToken ct = default)
    {
        var path = NormalizeDownloadPath(downloadPath);

        _logger.Debug("Downloading attachment from '{Path}'.", path);
        var response = await _http.GetAsync(path, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(ct);
    }

    /// <summary>Normalizes attachment download paths: absolute URLs pass through,
    /// relative paths get the API base prefix if needed (TKT-004).</summary>
    private string NormalizeDownloadPath(string downloadPath)
    {
        // Absolute HTTP URL → use as-is (HttpClient handles auth via default headers)
        if (Uri.TryCreate(downloadPath, UriKind.Absolute, out var uri)
            && uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return downloadPath;
        }

        // Already prefixed with API base → use as-is
        if (downloadPath.StartsWith(_apiBase, StringComparison.OrdinalIgnoreCase))
        {
            return downloadPath;
        }

        // Relative path → prepend API base
        return $"{_apiBase}{downloadPath}";
    }

    // ─── HTTP Helpers ───────────────────────────────────────────────────────

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        var response = await _http.GetAsync(path, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct)
               ?? throw new InvalidOperationException($"Failed to deserialize response from {path}.");
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string path, TRequest body, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync(path, body, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct)
               ?? throw new InvalidOperationException($"Failed to deserialize response from {path}.");
    }

    private async Task<TResponse> PutAsync<TRequest, TResponse>(
        string path, TRequest body, CancellationToken ct)
    {
        var response = await _http.PutAsJsonAsync(path, body, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct)
               ?? throw new InvalidOperationException($"Failed to deserialize response from {path}.");
    }

    private async IAsyncEnumerable<T> FetchPaginatedAsync<T>(
        string initialPath, [EnumeratorCancellation] CancellationToken ct)
    {
        string? path = initialPath;
        while (path is not null)
        {
            var result = await GetAsync<ConfluencePaginatedResult<T>>(path, ct);
            foreach (var item in result.Results)
            {
                yield return item;
            }
            path = result.Links?.Next;
        }
    }

    // ─── Labels (v1 API) ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<string>> GetPageLabelsAsync(string pageId, CancellationToken ct = default)
    {
        _logger.Debug("Fetching labels for page '{Id}'.", pageId);
        var result = await GetAsync<ConfluencePaginatedResult<ConfluenceLabel>>(
            $"{_apiBase}/rest/api/content/{pageId}/label", ct);
        return result.Results.Select(l => l.Name).ToList();
    }

    public async Task AddPageLabelsAsync(string pageId, IEnumerable<string> labels, CancellationToken ct = default)
    {
        var labelList = labels.Select(l => new { prefix = "global", name = l }).ToArray();
        if (labelList.Length == 0) return;

        _logger.Information("Adding {Count} label(s) to page '{Id}'.", labelList.Length, pageId);
        var response = await _http.PostAsJsonAsync(
            $"{_apiBase}/rest/api/content/{pageId}/label", labelList, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
    }



    // ─── Content Properties (v2 API) ────────────────────────────────────────

    public async Task SetContentPropertyAsync(
        string pageId, string key, string value, CancellationToken ct = default)
    {
        _logger.Debug("Setting property '{Key}' on page '{Id}'.", key, pageId);
        var body = new { key, value };
        var response = await _http.PostAsJsonAsync(
            $"{_v2}/pages/{pageId}/properties", body, JsonOptions, ct);

        // If conflict (property exists), try to update it
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // Get existing property to get its version
            var existing = await GetAsync<ConfluencePropertyResult>(
                $"{_v2}/pages/{pageId}/properties?key={Uri.EscapeDataString(key)}", ct);

            if (existing.Results.Count > 0)
            {
                var prop = existing.Results[0];
                var updateBody = new { key, value, version = new { number = prop.Version.Number + 1 } };
                var updateResponse = await _http.PutAsJsonAsync(
                    $"{_v2}/pages/{pageId}/properties/{prop.Id}", updateBody, JsonOptions, ct);
                updateResponse.EnsureSuccessStatusCode();
            }
        }
        else
        {
            response.EnsureSuccessStatusCode();
        }
    }


}
