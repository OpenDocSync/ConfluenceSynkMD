using FluentAssertions;
using ConfluenceSynkMD.Services;

namespace ConfluenceSynkMD.Tests.Services;

public class SlugGeneratorTests
{

    [Fact]
    public void GenerateSlug_Should_ReturnLowercased_When_TitleHasMixedCase()
    {
        // Arrange
        const string title = "My Page Title";

        // Act
        var result = SlugGenerator.GenerateSlug(title);

        // Assert
        result.Should().Be("my-page-title");
    }

    [Fact]
    public void GenerateSlug_Should_ReplaceSpecialChars_When_TitleHasPunctuation()
    {
        // Arrange
        const string title = "Hello, World! (2024)";

        // Act
        var result = SlugGenerator.GenerateSlug(title);

        // Assert
        result.Should().Be("hello-world-2024");
    }

    [Fact]
    public void GenerateSlug_Should_CollapseDashes_When_TitleHasConsecutiveSpecials()
    {
        // Arrange
        const string title = "foo---bar___baz";

        // Act
        var result = SlugGenerator.GenerateSlug(title);

        // Assert
        result.Should().Be("foo-bar-baz");
    }

    [Fact]
    public void GenerateSlug_Should_TrimDashes_When_ResultHasLeadingOrTrailing()
    {
        // Arrange
        const string title = "---leading and trailing---";

        // Act
        var result = SlugGenerator.GenerateSlug(title);

        // Assert
        result.Should().Be("leading-and-trailing");
    }

    [Fact]
    public void GenerateSlug_Should_ReturnUntitled_When_TitleIsEmpty()
    {
        // Arrange
        const string title = "";

        // Act
        var result = SlugGenerator.GenerateSlug(title);

        // Assert
        result.Should().Be("untitled");
    }

    [Fact]
    public void GenerateSlug_Should_ReturnUntitled_When_TitleIsOnlySpecialChars()
    {
        // Arrange
        const string title = "!!!@@@###";

        // Act
        var result = SlugGenerator.GenerateSlug(title);

        // Assert
        result.Should().Be("untitled");
    }
}
