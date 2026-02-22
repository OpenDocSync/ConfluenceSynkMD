using Markdig.Extensions.Mathematics;
using Markdig.Renderers;

namespace ConfluentSynkMD.Markdig.Renderers;

/// <summary>
/// Renders inline math (<c>$...$</c>) as the Confluence <c>eazy-math-inline</c> structured macro.
/// Mirrors md2conf's <c>_transform_inline_math()</c>.
///
/// When <c>--render-latex</c> is active, the formula would instead be rendered as an image
/// (handled by the existing diagram pipeline). This renderer handles the non-rendered
/// pass-through case using the "LaTeX Math for Confluence" marketplace extension.
/// </summary>
public sealed class MathInlineRenderer : MarkdownObjectRenderer<ConfluenceRenderer, MathInline>
{
    protected override void Write(ConfluenceRenderer renderer, MathInline math)
    {
        var content = math.Content.ToString();
        if (string.IsNullOrEmpty(content)) return;

        // If --render-latex is enabled, delegate to the diagram pipeline
        if (renderer.ConverterOptions.RenderLatex)
        {
            // Emit as an image attachment reference (same as diagram blocks)
            var hash = ComputeShortHash(content);
            var filename = $"formula-{hash}.png";
            renderer.LatexFormulas.Add((filename, content));
            renderer.Write($"<ac:image><ri:attachment ri:filename=\"{EscapeAttr(filename)}\"/></ac:image>");
            return;
        }

        var localId = Guid.NewGuid().ToString();
        var macroId = Guid.NewGuid().ToString();

        var alignment = renderer.LayoutOptions.ImageAlignment ?? "center";

        renderer.Write($"<ac:structured-macro ac:name=\"eazy-math-inline\" ac:schema-version=\"1\"");
        renderer.Write($" ac:local-id=\"{localId}\" ac:macro-id=\"{macroId}\">");
        renderer.Write($"<ac:parameter ac:name=\"body\">{EscapeXml(content)}</ac:parameter>");
        renderer.Write($"<ac:parameter ac:name=\"align\">{EscapeXml(alignment)}</ac:parameter>");
        renderer.Write("</ac:structured-macro>");
    }

    private static string ComputeShortHash(string content)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes)[..8].ToLowerInvariant();
    }

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
            .Replace("\"", "&quot;");

    private static string EscapeAttr(string text) =>
        text.Replace("&", "&amp;").Replace("\"", "&quot;");
}

/// <summary>
/// Renders block math (<c>$$...$$</c>) as the Confluence <c>easy-math-block</c> structured macro.
/// Mirrors md2conf's <c>_transform_block_math()</c>.
/// </summary>
public sealed class MathBlockRenderer : MarkdownObjectRenderer<ConfluenceRenderer, MathBlock>
{
    protected override void Write(ConfluenceRenderer renderer, MathBlock math)
    {
        // Extract content from Lines
        var lines = math.Lines;
        var content = string.Empty;
        if (lines.Lines is not null)
        {
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines.Lines[i];
                var slice = line.Slice;
                sb.Append(slice.Text, slice.Start, slice.Length);
                if (i < lines.Count - 1)
                    sb.AppendLine();
            }
            content = sb.ToString().Trim();
        }

        if (string.IsNullOrEmpty(content)) return;

        // If --render-latex is enabled, delegate to the diagram pipeline
        if (renderer.ConverterOptions.RenderLatex)
        {
            var hash = ComputeShortHash(content);
            var filename = $"formula-{hash}.png";
            renderer.LatexFormulas.Add((filename, content));
            renderer.Write($"<ac:image><ri:attachment ri:filename=\"{EscapeAttr(filename)}\"/></ac:image>");
            return;
        }

        var localId = Guid.NewGuid().ToString();
        var macroId = Guid.NewGuid().ToString();

        var alignment = renderer.LayoutOptions.ImageAlignment ?? "center";

        renderer.Write($"<ac:structured-macro ac:name=\"easy-math-block\" ac:schema-version=\"1\"");
        renderer.Write($" data-layout=\"default\" ac:local-id=\"{localId}\" ac:macro-id=\"{macroId}\">");
        renderer.Write($"<ac:parameter ac:name=\"body\">{EscapeXml(content)}</ac:parameter>");
        renderer.Write($"<ac:parameter ac:name=\"align\">{EscapeXml(alignment)}</ac:parameter>");
        renderer.Write("</ac:structured-macro>");
    }

    private static string ComputeShortHash(string content)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes)[..8].ToLowerInvariant();
    }

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
            .Replace("\"", "&quot;");

    private static string EscapeAttr(string text) =>
        text.Replace("&", "&amp;").Replace("\"", "&quot;");
}
