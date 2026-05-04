using System.Text;

namespace ConfluenceSynkMD.Services;

/// <summary>
/// Parses renderer command env vars (DRAWIO_CMD, PLANTUML_CMD, LATEX_PDFLATEX_CMD,
/// LATEX_GHOSTSCRIPT_CMD) as shell-style "command + args" strings.
///
/// The original renderers passed the env var verbatim into
/// <see cref="System.Diagnostics.ProcessStartInfo.FileName"/>, which broke
/// multi-token values such as
/// <c>DRAWIO_CMD="drawio --no-sandbox --disable-gpu"</c> — Process.Start
/// looks for a literal file named <c>"drawio --no-sandbox --disable-gpu"</c>
/// and fails. This helper splits the value into a (FileName, ArgsPrefix) pair
/// so callers can spawn the binary and prepend the env-supplied flags before
/// their per-call arguments.
/// </summary>
public static class RendererCommandResolver
{
    /// <summary>
    /// Tokenizes <paramref name="envValue"/> with shell-like quoting rules
    /// (double quotes group whitespace into a single token; backslash and
    /// single-quote handling are intentionally NOT supported — env vars are
    /// project config, not arbitrary shell input).
    /// </summary>
    /// <returns>
    /// A pair where <c>FileName</c> is the first token (the executable to
    /// spawn) and <c>ArgsPrefix</c> is the remaining tokens joined by a single
    /// space (or empty when the env value is a single bare command).
    /// Throws <see cref="ArgumentException"/> when the value is empty or
    /// whitespace-only.
    /// </returns>
    public static (string FileName, string ArgsPrefix) Parse(string envValue)
    {
        if (string.IsNullOrWhiteSpace(envValue))
            throw new ArgumentException("Renderer command env value is empty.", nameof(envValue));

        var tokens = Tokenize(envValue);
        if (tokens.Count == 0)
            throw new ArgumentException("Renderer command env value contains no tokens.", nameof(envValue));

        var fileName = tokens[0];
        var argsPrefix = tokens.Count > 1
            ? string.Join(' ', tokens.Skip(1))
            : string.Empty;

        return (fileName, argsPrefix);
    }

    /// <summary>
    /// Joins an env-supplied argument prefix with a per-call argument suffix,
    /// preserving spacing semantics. Either side may be empty.
    /// </summary>
    public static string CombineArgs(string envArgsPrefix, string callArgs)
    {
        if (string.IsNullOrEmpty(envArgsPrefix)) return callArgs;
        if (string.IsNullOrEmpty(callArgs)) return envArgsPrefix;
        return $"{envArgsPrefix} {callArgs}";
    }

    private static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var inQuote = false;

        foreach (var c in input)
        {
            if (c == '"')
            {
                inQuote = !inQuote;
                continue;
            }

            if (char.IsWhiteSpace(c) && !inQuote)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return tokens;
    }
}
