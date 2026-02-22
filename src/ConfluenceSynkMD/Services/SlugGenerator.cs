using System.Text.RegularExpressions;

namespace ConfluenceSynkMD.Services;

/// <summary>
/// Generates URL-safe slugs from page titles for use as filenames.
/// </summary>
public sealed partial class SlugGenerator
{
    /// <summary>
    /// Converts a title into a URL-safe filename slug.
    /// Example: "My Page Title!" → "my-page-title"
    /// </summary>
    public static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant().Trim();
        slug = NonAlphanumericPattern().Replace(slug, "-");
        slug = MultipleDashPattern().Replace(slug, "-");
        slug = slug.Trim('-');
        return string.IsNullOrEmpty(slug) ? "untitled" : slug;
    }

    [GeneratedRegex(@"[^a-z0-9\-]", RegexOptions.Compiled)]
    private static partial Regex NonAlphanumericPattern();

    [GeneratedRegex(@"-{2,}", RegexOptions.Compiled)]
    private static partial Regex MultipleDashPattern();
}
