using FluentAssertions;
using ConfluenceSynkMD.Services;

namespace ConfluenceSynkMD.Tests.Services;

public class AdmonitionPreProcessorTests
{
    [Fact]
    public void ConvertAdmonitions_Should_ConvertInfoToNote_When_BasicAdmonition()
    {
        // Arrange
        var input = "!!! info\n    This is an info block.\n";

        // Act
        var result = AdmonitionPreProcessor.ConvertAdmonitions(input);

        // Assert
        result.Should().Contain("> [!NOTE]");
        result.Should().Contain("> This is an info block.");
    }

    [Fact]
    public void ConvertAdmonitions_Should_IncludeTitle_When_QuotedTitlePresent()
    {
        // Arrange
        var input = "!!! tip \"Pro Tip\"\n    Use caching wisely.\n";

        // Act
        var result = AdmonitionPreProcessor.ConvertAdmonitions(input);

        // Assert
        result.Should().Contain("> [!TIP]");
        result.Should().Contain("> **Pro Tip**");
        result.Should().Contain("> Use caching wisely.");
    }

    [Theory]
    [InlineData("info", "NOTE")]
    [InlineData("note", "NOTE")]
    [InlineData("tip", "TIP")]
    [InlineData("hint", "TIP")]
    [InlineData("important", "IMPORTANT")]
    [InlineData("warning", "WARNING")]
    [InlineData("danger", "CAUTION")]
    [InlineData("caution", "CAUTION")]
    [InlineData("bug", "CAUTION")]
    [InlineData("todo", "IMPORTANT")]
    [InlineData("success", "TIP")]
    public void ConvertAdmonitions_Should_MapTypesCorrectly_When_VariousTypes(string mkdocsType, string expectedAlert)
    {
        // Arrange
        var input = $"!!! {mkdocsType}\n    Body text.\n";

        // Act
        var result = AdmonitionPreProcessor.ConvertAdmonitions(input);

        // Assert
        result.Should().Contain($"> [!{expectedAlert}]");
    }

    [Fact]
    public void ConvertAdmonitions_Should_IncludeMultilineBody_When_IndentedContent()
    {
        // Arrange
        var input = "!!! warning\n    Line one.\n    Line two.\n    Line three.\n";

        // Act
        var result = AdmonitionPreProcessor.ConvertAdmonitions(input);

        // Assert
        result.Should().Contain("> Line one.");
        result.Should().Contain("> Line two.");
        result.Should().Contain("> Line three.");
    }

    [Fact]
    public void ConvertAdmonitions_Should_PassThrough_When_NoAdmonitionsPresent()
    {
        // Arrange
        const string input = "# Hello World\n\nThis is regular markdown.\n";

        // Act
        var result = AdmonitionPreProcessor.ConvertAdmonitions(input);

        // Assert
        result.Should().Contain("# Hello World");
        result.Should().Contain("This is regular markdown.");
    }

    [Fact]
    public void ConvertAdmonitions_Should_HandleTabIndent_When_BodyUsesTabs()
    {
        // Arrange
        var input = "!!! note\n\tTabbed content.\n";

        // Act
        var result = AdmonitionPreProcessor.ConvertAdmonitions(input);

        // Assert
        result.Should().Contain("> [!NOTE]");
        result.Should().Contain("> Tabbed content.");
    }

    [Fact]
    public void ConvertAdmonitions_Should_ReturnUnchanged_When_EmptyInput()
    {
        // Arrange
        const string input = "";

        // Act
        var result = AdmonitionPreProcessor.ConvertAdmonitions(input);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ConvertAdmonitions_Should_HandleConsecutive_When_MultipleBackToBack()
    {
        // Arrange
        var input = "!!! info\n    First block.\n\n!!! warning\n    Second block.\n";

        // Act
        var result = AdmonitionPreProcessor.ConvertAdmonitions(input);

        // Assert
        result.Should().Contain("> [!NOTE]");
        result.Should().Contain("> First block.");
        result.Should().Contain("> [!WARNING]");
        result.Should().Contain("> Second block.");
    }
}
