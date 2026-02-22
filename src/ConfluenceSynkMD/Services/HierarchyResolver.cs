using ConfluenceSynkMD.Models;
using Serilog;

namespace ConfluenceSynkMD.Services;

/// <summary>
/// Resolves the directory structure into a tree of DocumentNodes,
/// identifying parent/child relationships.
/// Mirrors md2conf's Processor._index_directory() from processor.py.
/// </summary>
public sealed class HierarchyResolver
{
    private readonly FrontmatterParser _frontmatterParser;
    private readonly ILogger _logger;

    /// <summary>Filenames that represent a parent (index) page for a directory.</summary>
    private static readonly HashSet<string> IndexFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "index.md", "readme.md"
    };

    /// <summary>Directory names that contain resources and should not be scanned for pages.</summary>
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "img", "images", "assets", ".git", "node_modules", "__pycache__", ".venv", "venv"
    };

    public HierarchyResolver(FrontmatterParser frontmatterParser, ILogger logger)
    {
        _frontmatterParser = frontmatterParser;
        _logger = logger.ForContext<HierarchyResolver>();
    }

    /// <summary>
    /// Scans the given root path and builds a tree of DocumentNodes.
    /// index.md / README.md become parent nodes; other .md files become children.
    /// </summary>
    public IReadOnlyList<DocumentNode> BuildTree(string rootPath)
    {
        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"Path not found: {rootPath}");

        return ScanDirectory(rootPath, rootPath);
    }

    private List<DocumentNode> ScanDirectory(string directory, string rootPath)
    {
        var nodes = new List<DocumentNode>();
        var mdFiles = Directory.GetFiles(directory, "*.md", SearchOption.TopDirectoryOnly);

        // Check for .mdignore
        var mdIgnorePath = Path.Combine(directory, ".mdignore");
        var ignored = File.Exists(mdIgnorePath)
            ? File.ReadAllLines(mdIgnorePath)
                  .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith('#'))
                  .Select(l => l.Trim())
                  .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Find index file (parent page for this directory)
        var indexFile = mdFiles.FirstOrDefault(f =>
            IndexFileNames.Contains(Path.GetFileName(f)));

        // Process child .md files (non-index)
        var childFiles = mdFiles
            .Where(f => f != indexFile && !ignored.Contains(Path.GetFileName(f)))
            .ToList();

        // Process subdirectories recursively (skip resource directories)
        var subDirs = Directory.GetDirectories(directory)
            .Where(d => !ignored.Contains(Path.GetFileName(d)))
            .Where(d => !IgnoredDirectories.Contains(Path.GetFileName(d)))
            .ToList();

        var children = new List<DocumentNode>();

        foreach (var file in childFiles)
        {
            var node = CreateNode(file, rootPath, Array.Empty<DocumentNode>());
            if (node is not null)
                children.Add(node);
        }

        foreach (var subDir in subDirs)
        {
            var subNodes = ScanDirectory(subDir, rootPath);
            children.AddRange(subNodes);
        }

        if (indexFile is not null)
        {
            // Index file becomes the parent, other files/dirs become children
            var parent = CreateNode(indexFile, rootPath, children);
            if (parent is not null)
                nodes.Add(parent);
        }
        else
        {
            // No index file – just add all children as top-level nodes
            nodes.AddRange(children);
        }

        return nodes;
    }

    private DocumentNode? CreateNode(
        string filePath, string rootPath, IReadOnlyList<DocumentNode> children)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            var (metadata, remainingText) = _frontmatterParser.Parse(content);

            // Skip files explicitly marked as not synchronized
            if (!metadata.Synchronized)
            {
                _logger.Debug("Skipping non-synchronized file: {Path}", filePath);
                return null;
            }

            var relativePath = Path.GetRelativePath(rootPath, filePath);

            return new DocumentNode(
                AbsolutePath: filePath,
                RelativePath: relativePath,
                Metadata: metadata,
                MarkdownContent: remainingText,
                Children: children);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to process file: {Path}", filePath);
            return null;
        }
    }
}
