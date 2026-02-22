using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using TaskListCheckbox = global::Markdig.Extensions.TaskLists.TaskList;

namespace ConfluentSynkMD.Markdig.Renderers;

/// <summary>
/// Renders ordered and unordered lists as standard HTML.
/// Also detects task list items and renders them as Confluence
/// ac:task-list macros. Mirrors md2conf's _transform_tasklist().
/// </summary>
public sealed class ListRenderer : MarkdownObjectRenderer<ConfluenceRenderer, ListBlock>
{
    protected override void Write(ConfluenceRenderer renderer, ListBlock list)
    {
        // Check if this is a task list
        if (IsTaskList(list))
        {
            RenderTaskList(renderer, list);
            return;
        }

        var tag = list.IsOrdered ? "ol" : "ul";
        renderer.WriteLine($"<{tag}>");

        foreach (var item in list)
        {
            if (item is not ListItemBlock listItem) continue;
            renderer.Write("<li>");
            renderer.WriteChildren(listItem);
            renderer.WriteLine("</li>");
        }

        renderer.WriteLine($"</{tag}>");
    }

    private static bool IsTaskList(ListBlock list)
    {
        foreach (var item in list)
        {
            if (item is ListItemBlock { Count: > 0 } li &&
                li[0] is ParagraphBlock para &&
                para.Inline?.FirstChild is TaskListCheckbox)
            {
                return true;
            }
        }
        return false;
    }

    private static void RenderTaskList(ConfluenceRenderer renderer, ListBlock list)
    {
        renderer.WriteLine("<ac:task-list>");

        foreach (var item in list)
        {
            if (item is not ListItemBlock listItem) continue;

            renderer.Write("<ac:task>");

            // Check for task list checkbox
            if (listItem.Count > 0 &&
                listItem[0] is ParagraphBlock para &&
                para.Inline?.FirstChild is TaskListCheckbox taskList)
            {
                var status = taskList.Checked ? "complete" : "incomplete";
                renderer.Write($"<ac:task-status>{status}</ac:task-status>");
                renderer.Write("<ac:task-body>");

                // Write remaining inline content after the checkbox
                var inline = para.Inline.FirstChild?.NextSibling;
                while (inline is not null)
                {
                    renderer.Write(inline);
                    inline = inline.NextSibling;
                }
                renderer.Write("</ac:task-body>");
            }
            else
            {
                renderer.Write("<ac:task-status>incomplete</ac:task-status>");
                renderer.Write("<ac:task-body>");
                renderer.WriteChildren(listItem);
                renderer.Write("</ac:task-body>");
            }

            renderer.WriteLine("</ac:task>");
        }

        renderer.WriteLine("</ac:task-list>");
    }
}
