using FluentAssertions;
using ConfluentSynkMD.Configuration;

namespace ConfluentSynkMD.Tests.Configuration;

public class LayoutOptionsTests
{
    [Fact]
    public void Defaults_Should_HaveExpectedValues()
    {
        var opts = new LayoutOptions();

        opts.TableDisplayMode.Should().Be("responsive");
        opts.ImageAlignment.Should().BeNull();
        opts.ImageMaxWidth.Should().BeNull();
        opts.TableWidth.Should().BeNull();
        opts.ContentAlignment.Should().BeNull();
    }

    [Fact]
    public void With_Should_CreateCorrectCopy()
    {
        var original = new LayoutOptions { ImageAlignment = "center", ImageMaxWidth = 800 };
        var copy = original with { TableWidth = 1200 };

        copy.ImageAlignment.Should().Be("center");
        copy.ImageMaxWidth.Should().Be(800);
        copy.TableWidth.Should().Be(1200);
    }
}
