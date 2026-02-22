using FluentAssertions;
using ConfluentSynkMD.Configuration;

namespace ConfluentSynkMD.Tests.Configuration;

public class ConfluenceSettingsValidatorTests
{
    [Fact]
    public void Valid_BasicAuth_DoesNotThrow()
    {
        var settings = new ConfluenceSettings
        {
            BaseUrl = "https://example.atlassian.net",
            AuthMode = "Basic",
            UserEmail = "user@example.com",
            ApiToken = "token123"
        };

        var act = () => ConfluenceSettingsValidator.ValidateOrThrow(settings);

        act.Should().NotThrow();
    }

    [Fact]
    public void Valid_BearerAuth_DoesNotThrow()
    {
        var settings = new ConfluenceSettings
        {
            BaseUrl = "https://example.atlassian.net",
            AuthMode = "Bearer",
            BearerToken = "bearer-token-xyz"
        };

        var act = () => ConfluenceSettingsValidator.ValidateOrThrow(settings);

        act.Should().NotThrow();
    }

    [Fact]
    public void AuthMode_IsCaseInsensitive()
    {
        var settings = new ConfluenceSettings
        {
            BaseUrl = "https://example.atlassian.net",
            AuthMode = "basic",
            UserEmail = "user@example.com",
            ApiToken = "token123"
        };

        var act = () => ConfluenceSettingsValidator.ValidateOrThrow(settings);

        act.Should().NotThrow();
    }

    [Fact]
    public void Missing_BaseUrl_Throws()
    {
        var settings = new ConfluenceSettings
        {
            BaseUrl = "",
            AuthMode = "Basic",
            UserEmail = "user@example.com",
            ApiToken = "token123"
        };

        var act = () => ConfluenceSettingsValidator.ValidateOrThrow(settings);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BaseUrl*");
    }

    [Fact]
    public void BasicAuth_Missing_Email_Throws()
    {
        var settings = new ConfluenceSettings
        {
            BaseUrl = "https://example.atlassian.net",
            AuthMode = "Basic",
            UserEmail = null,
            ApiToken = "token123"
        };

        var act = () => ConfluenceSettingsValidator.ValidateOrThrow(settings);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*UserEmail*");
    }

    [Fact]
    public void BasicAuth_Missing_ApiToken_Throws()
    {
        var settings = new ConfluenceSettings
        {
            BaseUrl = "https://example.atlassian.net",
            AuthMode = "Basic",
            UserEmail = "user@example.com",
            ApiToken = null
        };

        var act = () => ConfluenceSettingsValidator.ValidateOrThrow(settings);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ApiToken*");
    }

    [Fact]
    public void BearerAuth_Missing_Token_Throws()
    {
        var settings = new ConfluenceSettings
        {
            BaseUrl = "https://example.atlassian.net",
            AuthMode = "Bearer",
            BearerToken = null
        };

        var act = () => ConfluenceSettingsValidator.ValidateOrThrow(settings);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BearerToken*");
    }

    [Fact]
    public void Unknown_AuthMode_Throws()
    {
        var settings = new ConfluenceSettings
        {
            BaseUrl = "https://example.atlassian.net",
            AuthMode = "NTLM"
        };

        var act = () => ConfluenceSettingsValidator.ValidateOrThrow(settings);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown AuthMode*NTLM*");
    }

    [Fact]
    public void Multiple_Missing_Fields_ReportsAll()
    {
        var settings = new ConfluenceSettings
        {
            BaseUrl = "",
            AuthMode = "Basic",
            UserEmail = null,
            ApiToken = null
        };

        var act = () => ConfluenceSettingsValidator.ValidateOrThrow(settings);

        act.Should().Throw<InvalidOperationException>()
            .And.Message.Should()
            .Contain("BaseUrl").And
            .Contain("UserEmail").And
            .Contain("ApiToken");
    }

    [Fact]
    public void BaseUrl_WithPath_AndMatchingApiPath_IsNormalized()
    {
        var settings = new ConfluenceSettings
        {
            BaseUrl = "https://example.atlassian.net/wiki/",
            ApiPath = "/wiki",
            AuthMode = "Basic",
            UserEmail = "user@example.com",
            ApiToken = "token123"
        };

        var act = () => ConfluenceSettingsValidator.ValidateOrThrow(settings);

        act.Should().NotThrow();
        settings.BaseUrl.Should().Be("https://example.atlassian.net");
        settings.ApiPath.Should().Be("/wiki");
    }

    [Fact]
    public void BaseUrl_WithPath_AndEmptyApiPath_MovesPathToApiPath()
    {
        var settings = new ConfluenceSettings
        {
            BaseUrl = "https://example.atlassian.net/wiki",
            ApiPath = "",
            AuthMode = "Basic",
            UserEmail = "user@example.com",
            ApiToken = "token123"
        };

        var act = () => ConfluenceSettingsValidator.ValidateOrThrow(settings);

        act.Should().NotThrow();
        settings.BaseUrl.Should().Be("https://example.atlassian.net");
        settings.ApiPath.Should().Be("/wiki");
    }

    [Fact]
    public void BaseUrl_WithPath_AndDifferentApiPath_Throws()
    {
        var settings = new ConfluenceSettings
        {
            BaseUrl = "https://example.atlassian.net/wiki",
            ApiPath = "/confluence",
            AuthMode = "Basic",
            UserEmail = "user@example.com",
            ApiToken = "token123"
        };

        var act = () => ConfluenceSettingsValidator.ValidateOrThrow(settings);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BaseUrl contains path*/wiki*ApiPath*/confluence*");
    }
}
