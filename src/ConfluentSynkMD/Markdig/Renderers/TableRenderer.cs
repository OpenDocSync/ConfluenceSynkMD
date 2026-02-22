using System.Globalization;
using System.Text;
using Markdig.Extensions.Tables;
using Markdig.Renderers;

namespace ConfluentSynkMD.Markdig.Renderers;

/// <summary>
/// Renders Markdig tables as standard HTML tables for Confluence.
/// Supports layout options for table width, display mode, and alignment.
/// Confluence Storage Format uses standard HTML table elements.
/// </summary>
public sealed class TableRenderer : MarkdownObjectRenderer<ConfluenceRenderer, Table>
{
    protected override void Write(ConfluenceRenderer renderer, Table table)
    {
        // Build table style from layout options
        var style = new StringBuilder();

        var tableWidth = renderer.LayoutOptions.TableWidth;
        if (tableWidth.HasValue && tableWidth.Value > 0)
        {
            style.Append(CultureInfo.InvariantCulture, $"width: {tableWidth.Value}px;");
        }

        var alignment = renderer.LayoutOptions.ContentAlignment;
        if (!string.IsNullOrEmpty(alignment)
            && !alignment.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            style.Append(CultureInfo.InvariantCulture, $" text-align: {alignment.ToLowerInvariant()};");
        }

        var displayMode = renderer.LayoutOptions.TableDisplayMode;
        if (displayMode.Equals("responsive", StringComparison.OrdinalIgnoreCase))
        {
            style.Append(" table-layout: auto;");
        }
        else if (displayMode.Equals("fixed", StringComparison.OrdinalIgnoreCase))
        {
            style.Append(" table-layout: fixed;");
        }

        if (style.Length > 0)
        {
            renderer.WriteLine($"<table style=\"{style.ToString().Trim()}\">");
        }
        else
        {
            renderer.WriteLine("<table>");
        }

        var isHeaderRow = true;
        foreach (var rowObj in table)
        {
            if (rowObj is not TableRow row) continue;

            renderer.Write("<tr>");
            foreach (var cellObj in row)
            {
                if (cellObj is not TableCell cell) continue;

                var tag = (isHeaderRow || row.IsHeader) ? "th" : "td";
                renderer.Write($"<{tag}>");
                renderer.WriteChildren(cell);
                renderer.Write($"</{tag}>");
            }
            renderer.WriteLine("</tr>");

            if (row.IsHeader) isHeaderRow = false;
        }

        renderer.WriteLine("</table>");
    }
}
