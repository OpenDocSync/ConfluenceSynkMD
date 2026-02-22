using FluentAssertions;
using ConfluentSynkMD.Configuration;

namespace ConfluentSynkMD.Tests.Configuration;

public class ConfluenceSettingsTests
{
    [Fact]
    public void Defaults_Should_HaveExpectedValues()
    {
        var settings = new ConfluenceSettings();

        settings.BaseUrl.Should().BeEmpty();
        settings.AuthMode.Should().Be("Basic");
        settings.UserEmail.Should().BeNull();
        settings.ApiToken.Should().BeNull();
        settings.BearerToken.Should().BeNull();
        settings.OptimizeImages.Should().BeTrue();
        settings.MaxImageWidth.Should().Be(1280);
        settings.ApiPath.Should().Be("/wiki");
        settings.ApiVersion.Should().Be("v2");
        settings.CustomHeaders.Should().BeEmpty();
    }

    [Fact]
    public void Properties_Should_BeSettable()
    {
        // Simulates CLI merge: set individual properties after construction
        var settings = new ConfluenceSettings();

        settings.BaseUrl = "https://test.atlassian.net";
        settings.AuthMode = "Bearer";
        settings.BearerToken = "tok-123";

        settings.BaseUrl.Should().Be("https://test.atlassian.net");
        settings.AuthMode.Should().Be("Bearer");
        settings.BearerToken.Should().Be("tok-123");
    }
}
