namespace ConfluenceSynkMD.Configuration;

/// <summary>
/// v0.1.0 introduced a CLI subcommand tree (`upload`, `download`, `local`)
/// and removed the legacy `--mode` flag and `--local` toggle. This helper
/// detects users running the old syntax and produces a friendly migration
/// hint instead of System.CommandLine's bare "Unrecognized option" error.
///
/// Forms detected:
///   <c>--mode Upload</c>          (space-separated)
///   <c>--mode=Upload</c>          (equals form)
///   <c>--mode:Upload</c>          (colon form, accepted by System.CommandLine)
///   <c>--local</c>                (the old standalone flag)
/// All matching is case-insensitive.
/// </summary>
public static class CliMigrationCheck
{
    /// <summary>
    /// Inspects raw process args for legacy CLI flags removed in v0.1.0.
    /// </summary>
    /// <param name="args">The args passed to the entry point.</param>
    /// <returns>
    /// A migration message + suggested subcommand name when a legacy flag is
    /// present in <paramref name="args"/>; otherwise <c>null</c>.
    /// </returns>
    public static MigrationHint? CheckLegacyModeFlag(string[] args)
    {
        if (args is null) return null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.IsNullOrEmpty(arg)) continue;

            // --mode <Value> (space-separated form)
            if (string.Equals(arg, "--mode", StringComparison.OrdinalIgnoreCase))
            {
                var modeValue = (i + 1 < args.Length) ? args[i + 1] : "<missing>";
                return BuildModeHint(modeValue);
            }

            // --mode=Value or --mode:Value (equals/colon forms)
            if (TryExtractInlineValue(arg, "--mode", out var inlineModeValue))
            {
                return BuildModeHint(inlineModeValue);
            }

            // --local (standalone flag, also removed in v0.1.0)
            if (string.Equals(arg, "--local", StringComparison.OrdinalIgnoreCase))
            {
                return BuildLocalHint();
            }
        }

        return null;
    }

    private static MigrationHint BuildModeHint(string modeValue)
    {
        var subcommand = MapModeValueToSubcommand(modeValue);
        var message =
            $"`--mode` was removed in v0.1.0. Use the `{subcommand}` subcommand instead:" + Environment.NewLine
            + $"  confluencesynkmd {subcommand} <other args...>" + Environment.NewLine
            + Environment.NewLine
            + "Migration table:" + Environment.NewLine
            + "  --mode Upload          ->  upload" + Environment.NewLine
            + "  --mode Download        ->  download" + Environment.NewLine
            + "  --mode LocalExport     ->  local" + Environment.NewLine
            + "  --mode Upload --local  ->  local" + Environment.NewLine;
        return new MigrationHint(subcommand, message);
    }

    private static MigrationHint BuildLocalHint()
    {
        var message =
            "`--local` was removed in v0.1.0. Use the `local` subcommand instead:" + Environment.NewLine
            + "  confluencesynkmd local <other args...>" + Environment.NewLine
            + Environment.NewLine
            + "Migration table:" + Environment.NewLine
            + "  --mode Upload          ->  upload" + Environment.NewLine
            + "  --mode Download        ->  download" + Environment.NewLine
            + "  --mode LocalExport     ->  local" + Environment.NewLine
            + "  --mode Upload --local  ->  local" + Environment.NewLine;
        return new MigrationHint("local", message);
    }

    private static bool TryExtractInlineValue(string arg, string flagPrefix, out string value)
    {
        // Match `--mode=...` and `--mode:...` (System.CommandLine accepts both).
        if (arg.Length > flagPrefix.Length
            && arg.StartsWith(flagPrefix, StringComparison.OrdinalIgnoreCase)
            && (arg[flagPrefix.Length] == '=' || arg[flagPrefix.Length] == ':'))
        {
            value = arg[(flagPrefix.Length + 1)..];
            if (string.IsNullOrEmpty(value)) value = "<missing>";
            return true;
        }

        value = string.Empty;
        return false;
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
