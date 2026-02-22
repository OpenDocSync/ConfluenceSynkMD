using FluentAssertions;
using ConfluentSynkMD.Services;
using Serilog;
using NSubstitute;

namespace ConfluentSynkMD.Tests.Services;

/// <summary>
/// Tests for <see cref="HierarchyResolver"/>.
/// Uses real filesystem via temp directories to verify actual directory-scanning behavior.
/// </summary>
public class HierarchyResolverTests : IDisposable
{
    private readonly HierarchyResolver _sut;
    private readonly string _tempDir;

    public HierarchyResolverTests()
    {
        var fmLogger = Substitute.For<ILogger>();
        var hrLogger = Substitute.For<ILogger>();
        var parser = new FrontmatterParser(fmLogger);
        _sut = new HierarchyResolver(parser, hrLogger);

        _tempDir = Path.Combine(Path.GetTempPath(), $"md2conf-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void BuildTree_Should_ReturnSingleNode_When_OneMarkdownFile()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, "page.md"), "# Hello\n\nSome content.");

        // Act
        var tree = _sut.BuildTree(_tempDir);

        // Assert
        tree.Should().HaveCount(1);
        tree[0].MarkdownContent.Should().Contain("Hello");
    }

    [Fact]
    public void BuildTree_Should_UseIndexAsParent_When_IndexMdExists()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, "index.md"), "# Parent\n\nI am the parent.");
        File.WriteAllText(Path.Combine(_tempDir, "child.md"), "# Child\n\nI am a child.");

        // Act
        var tree = _sut.BuildTree(_tempDir);

        // Assert
        tree.Should().HaveCount(1, because: "index.md becomes the parent node");
        tree[0].MarkdownContent.Should().Contain("Parent");
        tree[0].Children.Should().HaveCount(1);
        tree[0].Children[0].MarkdownContent.Should().Contain("Child");
    }

    [Fact]
    public void BuildTree_Should_IgnoreImgDirectory_When_ResourceDir()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, "page.md"), "# Main");
        var imgDir = Path.Combine(_tempDir, "img");
        Directory.CreateDirectory(imgDir);
        File.WriteAllText(Path.Combine(imgDir, "hidden.md"), "# Should be ignored");

        // Act
        var tree = _sut.BuildTree(_tempDir);

        // Assert
        tree.Should().HaveCount(1);
        tree[0].RelativePath.Should().NotContain("img");
    }

    [Fact]
    public void BuildTree_Should_ThrowDirectoryNotFound_When_PathMissing()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDir, "does-not-exist");

        // Act
        var act = () => _sut.BuildTree(nonExistentPath);

        // Assert
        act.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void BuildTree_Should_SkipFile_When_ListedInMdignore()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, "visible.md"), "# Visible");
        File.WriteAllText(Path.Combine(_tempDir, "hidden.md"), "# Hidden");
        File.WriteAllText(Path.Combine(_tempDir, ".mdignore"), "hidden.md");

        // Act
        var tree = _sut.BuildTree(_tempDir);

        // Assert
        tree.Should().HaveCount(1);
        tree[0].RelativePath.Should().Contain("visible");
    }

    [Fact]
    public void BuildTree_Should_NestChildren_When_SubdirectoriesExist()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, "index.md"), "# Root");
        var subDir = Path.Combine(_tempDir, "subdir");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "index.md"), "# Sub Parent");
        File.WriteAllText(Path.Combine(subDir, "leaf.md"), "# Leaf");

        // Act
        var tree = _sut.BuildTree(_tempDir);

        // Assert
        tree.Should().HaveCount(1);
        var rootChildren = tree[0].Children;
        rootChildren.Should().ContainSingle(c => c.Children.Count > 0,
            because: "subdir should have index.md as parent with leaf.md as child");
    }

    [Fact]
    public void BuildTree_Should_SkipNonSynchronized_When_FrontmatterSaysFalse()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, "synced.md"), "# Synced\n\nNormal page.");
        File.WriteAllText(Path.Combine(_tempDir, "skipped.md"),
            "---\nsynchronized: false\n---\n\n# Draft\n\nNot ready yet.");

        // Act
        var tree = _sut.BuildTree(_tempDir);

        // Assert
        tree.Should().HaveCount(1);
        tree[0].RelativePath.Should().Contain("synced");
    }

    [Fact]
    public void BuildTree_Should_ReturnEmpty_When_NoMarkdownFiles()
    {
        // Arrange — empty temp dir, no .md files

        // Act
        var tree = _sut.BuildTree(_tempDir);

        // Assert
        tree.Should().BeEmpty();
    }
}
