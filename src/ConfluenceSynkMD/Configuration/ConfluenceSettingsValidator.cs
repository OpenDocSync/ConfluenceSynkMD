namespace ConfluenceSynkMD.Configuration;

/// <summary>
/// Validates <see cref="ConfluenceSettings"/> before pipeline execution.
/// Throws <see cref="InvalidOperationException"/> with a clear message listing all missing fields.
/// </summary>
public static class ConfluenceSettingsValidator
{
    /// <summary>
    /// Validates that all required fields are set for the configured <see cref="ConfluenceSettings.AuthMode"/>.
    /// Call this before pipeline execution for any non-local mode.
    /// </summary>
    /// <param name="settings">The settings to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when required fields are missing or AuthMode is unknown.</exception>
    public static void ValidateOrThrow(ConfluenceSettings settings)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(settings.BaseUrl))
            errors.Add("BaseUrl is required. Set via --conf-base-url or CONFLUENCE__BASEURL.");
        else
            NormalizeBaseUrlAndApiPath(settings, errors);

        var authMode = settings.AuthMode?.Trim();

        if (string.Equals(authMode, "Basic", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(settings.UserEmail))
                errors.Add("UserEmail is required for Basic auth. Set via --conf-user-email or CONFLUENCE__USEREMAIL.");

            if (string.IsNullOrWhiteSpace(settings.ApiToken))
                errors.Add("ApiToken is required for Basic auth. Set via --conf-api-token or CONFLUENCE__APITOKEN.");
        }
        else if (string.Equals(authMode, "Bearer", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(settings.BearerToken))
                errors.Add("BearerToken is required for Bearer auth. Set via --conf-bearer-token or CONFLUENCE__BEARERTOKEN.");
        }
        else
        {
            errors.Add($"Unknown AuthMode '{settings.AuthMode}'. Must be 'Basic' or 'Bearer'.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Confluence configuration is incomplete:\n" +
                string.Join("\n", errors.Select(e => $"  • {e}")));
        }
    }

    private static void NormalizeBaseUrlAndApiPath(ConfluenceSettings settings, List<string> errors)
    {
        if (!Uri.TryCreate(settings.BaseUrl.Trim(), UriKind.Absolute, out var baseUri)
            || !string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("BaseUrl must be a valid absolute HTTPS URL (e.g. https://yoursite.atlassian.net).");
            return;
        }

        if (!string.IsNullOrWhiteSpace(baseUri.Query) || !string.IsNullOrWhiteSpace(baseUri.Fragment))
            errors.Add("BaseUrl must not contain query string or fragment.");

        var normalizedApiPath = NormalizeApiPath(settings.ApiPath);
        var basePath = baseUri.AbsolutePath.Trim('/');
        var origin = $"{baseUri.Scheme}://{baseUri.Authority}";

        if (!string.IsNullOrEmpty(basePath))
        {
            var basePathSegment = $"/{basePath}";

            if (string.IsNullOrEmpty(normalizedApiPath))
            {
                normalizedApiPath = basePathSegment;
            }
            else if (!string.Equals(normalizedApiPath, basePathSegment, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(
                    $"BaseUrl contains path '{basePathSegment}' while ApiPath is '{normalizedApiPath}'. " +
                    "Use a host-only BaseUrl or make ApiPath match the BaseUrl path.");
            }
        }

        settings.BaseUrl = origin;
        settings.ApiPath = normalizedApiPath;
    }

    private static string NormalizeApiPath(string? apiPath)
    {
        var trimmed = apiPath?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed) || trimmed == "/")
            return string.Empty;

        var segment = trimmed.Trim('/');
        return string.IsNullOrEmpty(segment) ? string.Empty : $"/{segment}";
    }
}
