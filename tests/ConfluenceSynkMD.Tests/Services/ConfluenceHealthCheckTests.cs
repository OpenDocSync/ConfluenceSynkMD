using System.Net;
using ConfluenceSynkMD.Configuration;
using ConfluenceSynkMD.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Serilog;

namespace ConfluenceSynkMD.Tests.Services;

/// <summary>
/// Covers every branch of <see cref="ConfluenceHealthCheck.CheckAsync"/>:
/// healthy v2, healthy via v1 fallback, auth error (401/403), upstream
/// error (5xx + v1-also-fails), network/timeout error, and the
/// pre-network NotConfigured shortcut. The four non-network cases use a
/// stub <see cref="HttpMessageHandler"/> so the test runs in isolation.
/// </summary>
public class ConfluenceHealthCheckTests
{
    private const string BaseUrl = "https://example.atlassian.net";
    private const string V2Path = "/wiki/api/v2/spaces?limit=1";
    private const string V1Path = "/wiki/rest/api/space?limit=1";

    [Fact]
    public async Task Returns_NotConfigured_when_BaseUrl_is_blank()
    {
        var settings = NewSettings(baseUrl: string.Empty);
        var probe = NewHealthCheck(settings, _ => new HttpResponseMessage(HttpStatusCode.OK));

        var result = await probe.CheckAsync();

        result.Status.Should().Be(HealthCheckStatus.NotConfigured);
        result.Detail.Should().Contain("BASEURL");
    }

    [Fact]
    public async Task Returns_NotConfigured_when_no_credentials_provided()
    {
        var settings = NewSettings(userEmail: string.Empty, apiToken: string.Empty);
        var probe = NewHealthCheck(settings, _ => new HttpResponseMessage(HttpStatusCode.OK));

        var result = await probe.CheckAsync();

        result.Status.Should().Be(HealthCheckStatus.NotConfigured);
        result.Detail.Should().ContainAny("USEREMAIL", "APITOKEN");
    }

    [Fact]
    public async Task Returns_Healthy_when_v2_endpoint_responds_200()
    {
        var settings = NewSettings();
        var probe = NewHealthCheck(settings, request =>
        {
            request.RequestUri!.AbsolutePath.Should().Be("/wiki/api/v2/spaces");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"results\":[]}"),
            };
        });

        var result = await probe.CheckAsync();

        result.Status.Should().Be(HealthCheckStatus.Healthy);
        result.Detail.Should().Contain("/api/v2/spaces");
    }

    [Fact]
    public async Task Returns_AuthError_on_401()
    {
        var settings = NewSettings();
        var probe = NewHealthCheck(settings, _ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"message\":\"Bad token\"}"),
        });

        var result = await probe.CheckAsync();

        result.Status.Should().Be(HealthCheckStatus.AuthError);
        result.Message.Should().Contain("401");
        result.Detail.Should().Contain("Bad token");
    }

    [Fact]
    public async Task Returns_AuthError_on_403()
    {
        var settings = NewSettings();
        var probe = NewHealthCheck(settings, _ => new HttpResponseMessage(HttpStatusCode.Forbidden));

        var result = await probe.CheckAsync();

        result.Status.Should().Be(HealthCheckStatus.AuthError);
        result.Message.Should().Contain("403");
    }

    [Fact]
    public async Task Falls_back_to_v1_when_v2_returns_404_and_returns_Healthy_when_v1_responds_200()
    {
        var settings = NewSettings();
        var calls = new List<string>();
        var probe = NewHealthCheck(settings, request =>
        {
            calls.Add(request.RequestUri!.AbsolutePath);
            if (request.RequestUri!.AbsolutePath.Contains("/api/v2/"))
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"results\":[]}"),
            };
        });

        var result = await probe.CheckAsync();

        result.Status.Should().Be(HealthCheckStatus.Healthy);
        result.Message.Should().Contain("v1 fallback");
        calls.Should().HaveCount(2);
        calls[0].Should().Contain("/api/v2/spaces");
        calls[1].Should().Contain("/rest/api/space");
    }

    [Fact]
    public async Task Returns_UpstreamError_when_both_v2_and_v1_return_4xx_or_5xx()
    {
        var settings = NewSettings();
        var probe = NewHealthCheck(settings, request =>
            request.RequestUri!.AbsolutePath.Contains("/api/v2/")
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("server exploded"),
                });

        var result = await probe.CheckAsync();

        result.Status.Should().Be(HealthCheckStatus.UpstreamError);
        result.Message.Should().Contain("v1 returned 500");
        result.Detail.Should().Contain("server exploded");
    }

    [Fact]
    public async Task Returns_UpstreamError_on_503()
    {
        var settings = NewSettings();
        var probe = NewHealthCheck(settings, _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var result = await probe.CheckAsync();

        result.Status.Should().Be(HealthCheckStatus.UpstreamError);
        result.Message.Should().Contain("503");
    }

    [Fact]
    public async Task Returns_NetworkError_when_HttpRequestException_thrown()
    {
        var settings = NewSettings();
        var probe = NewHealthCheck(settings, _ =>
            throw new HttpRequestException("dns lookup failed"));

        var result = await probe.CheckAsync();

        result.Status.Should().Be(HealthCheckStatus.NetworkError);
        result.Detail.Should().Contain("dns lookup failed");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IOptions<ConfluenceSettings> NewSettings(
        string baseUrl = BaseUrl,
        string userEmail = "user@example.com",
        string apiToken = "token-xyz",
        string apiPath = "/wiki",
        string apiVersion = "v2",
        string authMode = "Basic",
        string? bearerToken = null)
    {
        var settings = new ConfluenceSettings
        {
            BaseUrl = baseUrl,
            UserEmail = userEmail,
            ApiToken = apiToken,
            ApiPath = apiPath,
            ApiVersion = apiVersion,
            AuthMode = authMode,
            BearerToken = bearerToken ?? string.Empty,
        };
        return Options.Create(settings);
    }

    private static ConfluenceHealthCheck NewHealthCheck(
        IOptions<ConfluenceSettings> settings,
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler);
        var logger = Substitute.For<ILogger>();
        logger.ForContext<ConfluenceHealthCheck>().Returns(logger);
        return new ConfluenceHealthCheck(http, settings, logger);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }
}
