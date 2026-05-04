using System.Net;
using System.Net.Http.Headers;
using System.Text;
using ConfluenceSynkMD.Configuration;
using Microsoft.Extensions.Options;
using Serilog;

namespace ConfluenceSynkMD.Services;

/// <summary>
/// Default <see cref="IConfluenceHealthCheck"/> implementation.
///
/// Strategy:
///   1. Validate <see cref="ConfluenceSettings"/> are populated. If not,
///      return <see cref="HealthCheckStatus.NotConfigured"/> without making
///      a network call.
///   2. Send <c>GET /wiki/api/v2/spaces?limit=1</c> with the configured
///      auth header.
///   3. On 401/403 return <see cref="HealthCheckStatus.AuthError"/>.
///   4. On 404, retry against the v1 endpoint
///      <c>GET /wiki/rest/api/space?limit=1</c> (some Server/DC instances
///      do not expose v2). If v1 returns 2xx, treat as
///      <see cref="HealthCheckStatus.Healthy"/> with a note in
///      <see cref="HealthCheckResult.Detail"/>.
///   5. Anything else maps to <see cref="HealthCheckStatus.UpstreamError"/>.
///   6. Network failures (timeout, DNS, TLS) map to
///      <see cref="HealthCheckStatus.NetworkError"/>.
///
/// Auth + URL setup mirrors <see cref="ConfluenceApiClient"/> intentionally —
/// reusing the same logic via a shared helper would have meant a wider
/// refactor of the existing client. v0.2 can lift the duplication if it
/// becomes a real maintenance burden.
/// </summary>
public sealed class ConfluenceHealthCheck : IConfluenceHealthCheck
{
    /// <summary>Default per-request timeout for the health probe.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly HttpClient _http;
    private readonly ConfluenceSettings _settings;
    private readonly ILogger _logger;
    private readonly TimeSpan _timeout;

    public ConfluenceHealthCheck(
        HttpClient http,
        IOptions<ConfluenceSettings> settings,
        ILogger logger)
        : this(http, settings, logger, DefaultTimeout) { }

    /// <summary>
    /// Test-only constructor that lets callers shorten the timeout. Production
    /// DI uses the public 3-arg constructor with the 10-second default.
    /// </summary>
    internal ConfluenceHealthCheck(
        HttpClient http,
        IOptions<ConfluenceSettings> settings,
        ILogger logger,
        TimeSpan timeout)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger.ForContext<ConfluenceHealthCheck>();
        _timeout = timeout;
    }

    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        if (!TryBuildUrls(out var v2Url, out var v1Url, out var configError))
        {
            return new HealthCheckResult
            {
                Status = HealthCheckStatus.NotConfigured,
                Message = "Confluence not configured.",
                Detail = configError,
            };
        }

        ConfigureAuthHeader();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_timeout);

        // ── v2 attempt ────────────────────────────────────────────────────
        HttpResponseMessage? v2Response;
        try
        {
            v2Response = await _http.GetAsync(v2Url, timeoutCts.Token);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            return new HealthCheckResult
            {
                Status = HealthCheckStatus.NetworkError,
                Message = $"Confluence probe timed out after {_timeout.TotalSeconds:F0}s.",
                Detail = ex.Message,
            };
        }
        catch (HttpRequestException ex)
        {
            return new HealthCheckResult
            {
                Status = HealthCheckStatus.NetworkError,
                Message = "Could not reach Confluence (network or TLS error).",
                Detail = ex.Message,
            };
        }

        try
        {
            // 2xx on v2 = healthy
            if (v2Response.IsSuccessStatusCode)
            {
                return new HealthCheckResult
                {
                    Status = HealthCheckStatus.Healthy,
                    Message = "Confluence reachable; v2 spaces endpoint responded.",
                    Detail = $"GET {v2Url} -> {(int)v2Response.StatusCode}",
                };
            }

            // 401/403 = auth issue (do NOT fall back to v1 — same auth would fail there too)
            if (v2Response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                var body = await SafeReadBodyAsync(v2Response, ct);
                return new HealthCheckResult
                {
                    Status = HealthCheckStatus.AuthError,
                    Message = $"Auth rejected ({(int)v2Response.StatusCode} {v2Response.StatusCode}).",
                    Detail = body,
                };
            }

            // 404 = v2 endpoint missing on this instance; try v1 fallback
            if (v2Response.StatusCode == HttpStatusCode.NotFound)
            {
                v2Response.Dispose();
                return await TryV1FallbackAsync(v1Url, timeoutCts.Token);
            }

            // Anything else = upstream error
            var upstreamBody = await SafeReadBodyAsync(v2Response, ct);
            return new HealthCheckResult
            {
                Status = HealthCheckStatus.UpstreamError,
                Message = $"Confluence returned unexpected status {(int)v2Response.StatusCode}.",
                Detail = upstreamBody,
            };
        }
        finally
        {
            v2Response?.Dispose();
        }
    }

    private async Task<HealthCheckResult> TryV1FallbackAsync(Uri v1Url, CancellationToken ct)
    {
        HttpResponseMessage? v1Response;
        try
        {
            v1Response = await _http.GetAsync(v1Url, ct);
        }
        catch (TaskCanceledException ex)
        {
            return new HealthCheckResult
            {
                Status = HealthCheckStatus.NetworkError,
                Message = "v1 fallback timed out.",
                Detail = ex.Message,
            };
        }
        catch (HttpRequestException ex)
        {
            return new HealthCheckResult
            {
                Status = HealthCheckStatus.NetworkError,
                Message = "v1 fallback failed (network or TLS error).",
                Detail = ex.Message,
            };
        }

        try
        {
            if (v1Response.IsSuccessStatusCode)
            {
                return new HealthCheckResult
                {
                    Status = HealthCheckStatus.Healthy,
                    Message = "Confluence reachable via v1 fallback (v2 returned 404).",
                    Detail = $"GET {v1Url} -> {(int)v1Response.StatusCode}",
                };
            }

            var body = await SafeReadBodyAsync(v1Response, ct);
            return new HealthCheckResult
            {
                Status = HealthCheckStatus.UpstreamError,
                Message = $"v2 returned 404 and v1 returned {(int)v1Response.StatusCode}.",
                Detail = body,
            };
        }
        finally
        {
            v1Response?.Dispose();
        }
    }

    private bool TryBuildUrls(out Uri v2Url, out Uri v1Url, out string? error)
    {
        v2Url = default!;
        v1Url = default!;
        error = null;

        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            error = "CONFLUENCE__BASEURL is not set.";
            return false;
        }

        var hasBasic = !string.IsNullOrWhiteSpace(_settings.UserEmail)
                       && !string.IsNullOrWhiteSpace(_settings.ApiToken);
        var hasBearer = !string.IsNullOrWhiteSpace(_settings.BearerToken);
        if (!hasBasic && !hasBearer)
        {
            error = _settings.AuthMode.Equals("Bearer", StringComparison.OrdinalIgnoreCase)
                ? "CONFLUENCE__BEARERTOKEN is not set."
                : "CONFLUENCE__USEREMAIL and/or CONFLUENCE__APITOKEN is not set.";
            return false;
        }

        if (!Uri.TryCreate(_settings.BaseUrl.Trim(), UriKind.Absolute, out var baseUri))
        {
            error = $"CONFLUENCE__BASEURL is not a valid absolute URL: '{_settings.BaseUrl}'.";
            return false;
        }

        var apiPath = (_settings.ApiPath ?? "/wiki").TrimEnd('/');
        if (apiPath.Length > 0 && !apiPath.StartsWith('/')) apiPath = "/" + apiPath;

        v2Url = new Uri(baseUri, $"{apiPath}/api/v2/spaces?limit=1");
        v1Url = new Uri(baseUri, $"{apiPath}/rest/api/space?limit=1");
        return true;
    }

    private void ConfigureAuthHeader()
    {
        if (_http.DefaultRequestHeaders.Authorization is not null) return;

        if (_settings.AuthMode.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.BearerToken);
        }
        else
        {
            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_settings.UserEmail}:{_settings.ApiToken}"));
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
        }

        if (!_http.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/json"))
        {
            _http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }

    private static async Task<string?> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            return string.IsNullOrWhiteSpace(body) ? null : Truncate(body, 1024);
        }
        catch
        {
            return null;
        }
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "… (truncated)";
}
