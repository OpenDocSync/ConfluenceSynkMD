namespace ConfluenceSynkMD.Configuration;

/// <summary>
/// Configuration for connecting to Confluence Cloud.
/// Loaded from environment variables or appsettings.
/// </summary>
public sealed class ConfluenceSettings
{
    public const string SectionName = "Confluence";

    /// <summary>Confluence Cloud base URL (e.g. "https://yoursite.atlassian.net").</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Authentication mode: "Basic" for email + API token,
    /// "Bearer" for OAuth 2.0 access token.
    /// </summary>
    public string AuthMode { get; set; } = "Basic";

    /// <summary>User email address (required for Basic auth).</summary>
    public string? UserEmail { get; set; }

    /// <summary>API token (required for Basic auth).</summary>
    public string? ApiToken { get; set; }

    /// <summary>OAuth 2.0 Bearer token (required for Bearer auth).</summary>
    public string? BearerToken { get; set; }

    /// <summary>Whether to optimize and downscale images before upload.</summary>
    public bool OptimizeImages { get; set; } = true;

    /// <summary>Maximum width for optimized images. Height will be scaled accordingly.</summary>
    public int MaxImageWidth { get; set; } = 1280;

    /// <summary>API path override (e.g. "/wiki" for Confluence Cloud, "" for Data Center).</summary>
    public string ApiPath { get; set; } = "/wiki";

    /// <summary>REST API version: "v1" or "v2" (default "v2" for Confluence Cloud).</summary>
    public string ApiVersion { get; set; } = "v2";

    /// <summary>Custom HTTP headers to send with every API request (KEY=VALUE pairs).</summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
}
