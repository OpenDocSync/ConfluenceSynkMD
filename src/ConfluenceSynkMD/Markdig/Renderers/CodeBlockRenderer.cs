using System.Security.Cryptography;
using System.Text;
using Markdig.Renderers;
using Markdig.Syntax;

namespace ConfluenceSynkMD.Markdig.Renderers;

/// <summary>
/// Renders fenced code blocks as Confluence 'code' structured macros.
/// Supports Mermaid, Draw.io, PlantUML, and LaTeX code blocks which are
/// intercepted and emitted as image references or special macros.
///
/// Non-diagram output format:
/// &lt;ac:structured-macro ac:name="code"&gt;
///   &lt;ac:parameter ac:name="language"&gt;python&lt;/ac:parameter&gt;
///   &lt;ac:plain-text-body&gt;&lt;![CDATA[...]]&gt;&lt;/ac:plain-text-body&gt;
/// &lt;/ac:structured-macro&gt;
///
/// Diagram output format:
/// &lt;ac:image&gt;&lt;ri:attachment ri:filename="diagram-{hash}.png"/&gt;&lt;/ac:image&gt;
/// </summary>
public sealed class CodeBlockRenderer : MarkdownObjectRenderer<ConfluenceRenderer, CodeBlock>
{
    /// <summary>
    /// Maps language names/aliases to Confluence-compatible language IDs.
    /// Mirrors Python md2conf's <c>_LANGUAGES</c> dictionary (88 entries).
    /// When <c>--force-valid-language</c> is enabled, languages not in this
    /// dictionary fall back to "none".
    /// </summary>
    private static readonly Dictionary<string, string> LanguageAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["abap"] = "abap",
        ["actionscript3"] = "actionscript3",
        ["ada"] = "ada",
        ["applescript"] = "applescript",
        ["arduino"] = "arduino",
        ["autoit"] = "autoit",
        ["bash"] = "bash",
        ["c"] = "c",
        ["c#"] = "c#",
        ["clojure"] = "clojure",
        ["coffeescript"] = "coffeescript",
        ["coldfusion"] = "coldfusion",
        ["cpp"] = "cpp",
        ["c++"] = "cpp",
        ["csharp"] = "c#",
        ["css"] = "css",
        ["cuda"] = "cuda",
        ["d"] = "d",
        ["dart"] = "dart",
        ["delphi"] = "delphi",
        ["diff"] = "diff",
        ["dockerfile"] = "dockerfile",
        ["elixir"] = "elixir",
        ["erl"] = "erl",
        ["erlang"] = "erl",
        ["fortran"] = "fortran",
        ["foxpro"] = "foxpro",
        ["gherkin"] = "gherkin",
        ["go"] = "go",
        ["graphql"] = "graphql",
        ["groovy"] = "groovy",
        ["handlebars"] = "handlebars",
        ["haskell"] = "haskell",
        ["haxe"] = "haxe",
        ["hcl"] = "hcl",
        ["html"] = "html",
        ["java"] = "java",
        ["javafx"] = "javafx",
        ["javascript"] = "js",
        ["js"] = "js",
        ["json"] = "json",
        ["jsx"] = "jsx",
        ["julia"] = "julia",
        ["kotlin"] = "kotlin",
        ["livescript"] = "livescript",
        ["lua"] = "lua",
        ["mathematica"] = "mathematica",
        ["matlab"] = "matlab",
        ["objectivec"] = "objectivec",
        ["objectivej"] = "objectivej",
        ["ocaml"] = "ocaml",
        ["octave"] = "octave",
        ["pascal"] = "pascal",
        ["perl"] = "perl",
        ["php"] = "php",
        ["powershell"] = "powershell",
        ["prolog"] = "prolog",
        ["protobuf"] = "protobuf",
        ["puppet"] = "puppet",
        ["py"] = "py",
        ["python"] = "py",
        ["qml"] = "qml",
        ["r"] = "r",
        ["racket"] = "racket",
        ["rst"] = "rst",
        ["ruby"] = "ruby",
        ["rust"] = "rust",
        ["sass"] = "sass",
        ["scala"] = "scala",
        ["scheme"] = "scheme",
        ["shell"] = "shell",
        ["smalltalk"] = "smalltalk",
        ["splunk"] = "splunk",
        ["sql"] = "sql",
        ["standardml"] = "standardml",
        ["swift"] = "swift",
        ["tcl"] = "tcl",
        ["tex"] = "tex",
        ["text"] = "none",
        ["plain"] = "none",
        ["none"] = "none",
        ["toml"] = "toml",
        ["tsx"] = "tsx",
        ["typescript"] = "typescript",
        ["vala"] = "vala",
        ["vb"] = "vb",
        ["verilog"] = "verilog",
        ["vhdl"] = "vhdl",
        ["xml"] = "xml",
        ["xquery"] = "xquery",
        ["yaml"] = "yaml",
    };

    protected override void Write(ConfluenceRenderer renderer, CodeBlock codeBlock)
    {
        var code = ExtractCode(codeBlock);

        // Determine language from fenced code block info
        string? language = null;
        if (codeBlock is FencedCodeBlock fenced && !string.IsNullOrWhiteSpace(fenced.Info))
        {
            language = fenced.Info.Trim();
        }

        // ── Mermaid diagrams ────────────────────────────────────────────────
        if (string.Equals(language, "mermaid", StringComparison.OrdinalIgnoreCase)
            && renderer.ConverterOptions.RenderMermaid)
        {
            RenderDiagramBlock(renderer, code, "mermaid", renderer.MermaidDiagrams);
            return;
        }

        // ── Draw.io diagrams ────────────────────────────────────────────────
        if (string.Equals(language, "drawio", StringComparison.OrdinalIgnoreCase)
            && renderer.ConverterOptions.RenderDrawio)
        {
            var ext = renderer.ConverterOptions.DiagramOutputFormat;
            RenderDiagramBlock(renderer, code, "drawio", renderer.DrawioDiagrams, ext);
            return;
        }

        // ── PlantUML diagrams ───────────────────────────────────────────────
        if ((string.Equals(language, "plantuml", StringComparison.OrdinalIgnoreCase)
             || string.Equals(language, "puml", StringComparison.OrdinalIgnoreCase))
            && renderer.ConverterOptions.RenderPlantuml)
        {
            var ext = renderer.ConverterOptions.DiagramOutputFormat;
            RenderDiagramBlock(renderer, code, "plantuml", renderer.PlantUmlDiagrams, ext);
            return;
        }

        // ── LaTeX formulas ──────────────────────────────────────────────────
        if ((string.Equals(language, "latex", StringComparison.OrdinalIgnoreCase)
             || string.Equals(language, "math", StringComparison.OrdinalIgnoreCase))
            && renderer.ConverterOptions.RenderLatex)
        {
            RenderDiagramBlock(renderer, code, "latex", renderer.LatexFormulas, "png");
            return;
        }

        // ── Standard code block → Confluence code macro ─────────────────────
        renderer.Write("<ac:structured-macro ac:name=\"code\">");

        if (!string.IsNullOrEmpty(language))
        {
            // Resolve alias → Confluence language ID
            var effectiveLanguage = LanguageAliases.TryGetValue(language, out var resolved)
                ? resolved
                : (renderer.ConverterOptions.ForceValidLanguage ? "none" : language);

            renderer.Write($"<ac:parameter ac:name=\"language\">{EscapeXml(effectiveLanguage)}</ac:parameter>");
        }

        // Line numbers
        if (renderer.ConverterOptions.CodeLineNumbers)
        {
            renderer.Write("<ac:parameter ac:name=\"linenumbers\">true</ac:parameter>");
        }

        renderer.Write("<ac:plain-text-body><![CDATA[");
        renderer.Write(code);
        renderer.WriteLine("]]></ac:plain-text-body>");
        renderer.WriteLine("</ac:structured-macro>");
    }

    /// <summary>
    /// Renders a diagram code block as an image attachment reference,
    /// preserving the original source in a collapsed code macro.
    /// </summary>
    private static void RenderDiagramBlock(
        ConfluenceRenderer renderer, string code, string diagramType,
        List<(string, string)> diagramList, string extension = "png")
    {
        var hash = ComputeShortHash(code);
        var filename = $"{diagramType}-{hash}.{extension}";
        diagramList.Add((filename, code));

        // Render the image reference
        renderer.Write($"<ac:image><ri:attachment ri:filename=\"{EscapeAttr(filename)}\"/></ac:image>");

        // Preserve original source in a collapsed code macro for round-trip fidelity
        renderer.Write("<ac:structured-macro ac:name=\"code\">");
        renderer.Write($"<ac:parameter ac:name=\"language\">{diagramType}</ac:parameter>");
        renderer.Write("<ac:parameter ac:name=\"collapse\">true</ac:parameter>");
        renderer.Write($"<ac:parameter ac:name=\"title\">{char.ToUpper(diagramType[0], System.Globalization.CultureInfo.InvariantCulture) + diagramType[1..]} Source (auto-generated)</ac:parameter>");
        renderer.Write("<ac:plain-text-body><![CDATA[");
        renderer.Write(code);
        renderer.Write("]]></ac:plain-text-body>");
        renderer.Write("</ac:structured-macro>");
    }

    private static string ExtractCode(LeafBlock block)
    {
        if (block.Lines.Lines is null) return string.Empty;

        var lines = block.Lines;
        var builder = new StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines.Lines[i];
            var slice = line.Slice;
            builder.Append(slice.Text, slice.Start, slice.Length);
            if (i < lines.Count - 1)
                builder.AppendLine();
        }
        return builder.ToString();
    }

    private static string ComputeShortHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes)[..8].ToLowerInvariant();
    }

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
            .Replace("\"", "&quot;");

    private static string EscapeAttr(string text) =>
        text.Replace("&", "&amp;").Replace("\"", "&quot;");
}
