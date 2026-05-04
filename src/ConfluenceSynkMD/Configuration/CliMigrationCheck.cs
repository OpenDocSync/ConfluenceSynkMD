namespace ConfluenceSynkMD.Configuration;

/// <summary>
/// v0.1.0 introduced a CLI subcommand tree (`upload`, `download`, `local`)
/// and removed the legacy `--mode` flag and `--local` toggle. This helper
/// detects users running the old syntax and produces a friendly migration
/// hint instead of System.CommandLine's bare "Unrecognized option" error.
/// </summary>
public static class CliMigrationCheck
{
    /// <summary>
    /// Inspects raw process args for a legacy `--mode &lt;value&gt;` flag.
    /// </summary>
    /// <param name="args">The args passed to the entry point.</param>
    /// <returns>
    /// A migration message + suggested subcommand name when `--mode` is
    /// present in <paramref name="args"/>; otherwise <c>null</c>.
    /// </returns>
    public static MigrationHint? CheckLegacyModeFlag(string[] args)
    {
        if (args is null) return null;

        for (var i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], "--mode", StringComparison.OrdinalIgnoreCase))
                continue;

            var modeValue = (i + 1 < args.Length) ? args[i + 1] : "<missing>";
            var subcommand = MapModeValueToSubcommand(modeValue);

            var message =
                $"`--mode` was removed in v0.1.0. Use the `{subcommand}` subcommand instead:" + Environment.NewLine
                + $"  confluencesynkmd {subcommand} <other args...>" + Environment.NewLine
                + "See the v0.1.0 CHANGELOG for the full migration table.";

            return new MigrationHint(subcommand, message);
        }

        return null;
    }

    private static string MapModeValueToSubcommand(string modeValue) =>
        modeValue.Equals("Upload", StringComparison.OrdinalIgnoreCase) ? "upload"
        : modeValue.Equals("Download", StringComparison.OrdinalIgnoreCase) ? "download"
        : modeValue.Equals("LocalExport", StringComparison.OrdinalIgnoreCase) ? "local"
        : "<upload|download|local>";

    /// <summary>
    /// Carries the suggested subcommand and the user-facing message for a
    /// detected legacy invocation. Both values are populated when
    /// <see cref="CheckLegacyModeFlag"/> returns a non-null result.
    /// </summary>
    public sealed record MigrationHint(string SuggestedSubcommand, string Message);
}
