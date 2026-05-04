namespace ConfluenceSynkMD.Services;

/// <summary>
/// Lightweight reachability + auth probe against the configured Confluence
/// instance. Used by the <c>doctor</c> and <c>init</c> subcommands to surface
/// a clear pass/fail before the user runs an actual upload or download.
/// </summary>
public interface IConfluenceHealthCheck
{
    /// <summary>
    /// Hits a low-cost spaces-listing endpoint (v2 by default with v1 fallback)
    /// and classifies the response. Never throws for predictable failure modes
    /// (4xx/5xx, missing config, network error) — those map to a
    /// <see cref="HealthCheckStatus"/> instead.
    /// </summary>
    Task<HealthCheckResult> CheckAsync(CancellationToken ct = default);
}

/// <summary>
/// Outcome classes for <see cref="IConfluenceHealthCheck.CheckAsync"/>.
/// </summary>
public enum HealthCheckStatus
{
    /// <summary>Auth + connectivity good. Confluence reachable; spaces endpoint responded 2xx.</summary>
    Healthy,

    /// <summary>Caller is missing credentials / base URL — no probe attempted.</summary>
    NotConfigured,

    /// <summary>Probe reached Confluence but auth was rejected (401/403).</summary>
    AuthError,

    /// <summary>Probe reached Confluence but got 5xx, 404 on both v2 and v1, or another unexpected status.</summary>
    UpstreamError,

    /// <summary>Probe never reached Confluence (DNS, TCP, TLS, timeout).</summary>
    NetworkError,
}

/// <summary>
/// Result envelope. <see cref="Status"/> is always populated.
/// <see cref="Message"/> is a one-liner safe to print at the CLI.
/// <see cref="Detail"/> is a longer string with response body, exception text,
/// or which API version answered — surfaced when the user wants to drill in.
/// </summary>
public sealed record HealthCheckResult
{
    public required HealthCheckStatus Status { get; init; }
    public required string Message { get; init; }
    public string? Detail { get; init; }
}
