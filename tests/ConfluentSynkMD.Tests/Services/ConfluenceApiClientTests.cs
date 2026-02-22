using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ConfluentSynkMD.Configuration;
using ConfluentSynkMD.Models;
using ConfluentSynkMD.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Serilog;

namespace ConfluentSynkMD.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ConfluenceApiClient"/>.
/// Uses a <see cref="MockHttpMessageHandler"/> to intercept HTTP calls and
/// return pre-configured JSON responses.
/// </summary>
public sealed class ConfluenceApiClientTests
{
    // ─── Test Infrastructure ─────────────────────────────────────────────────

    private readonly ILogger _logger = Substitute.For<ILogger>();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private ConfluenceApiClient CreateSut(
        MockHttpMessageHandler handler,
        ConfluenceSettings? settings = null)
    {
        _logger.ForContext<ConfluenceApiClient>().Returns(_logger);
        var httpClient = new HttpClient(handler);
        settings ??= CreateSettings();
        return new ConfluenceApiClient(httpClient, Options.Create(settings), _logger);
    }

    private static ConfluenceSettings CreateSettings(
        string authMode = "Basic",
        string? bearerToken = null) => new()
        {
            BaseUrl = "https://example.atlassian.net",
            AuthMode = authMode,
            UserEmail = "test@example.com",
            ApiToken = "token",
            BearerToken = bearerToken,
            ApiPath = "/wiki",
            ApiVersion = "v2"
        };

    /// <summary>Serializes an object to a JSON StringContent with application/json media type.</summary>
    private static StringContent JsonContent(object obj) =>
        new(JsonSerializer.Serialize(obj, JsonOpts), Encoding.UTF8, "application/json");

    /// <summary>Wraps items in a paginated result envelope with no next link.</summary>
    private static object PaginatedResult<T>(IReadOnlyList<T> results, string? nextLink = null) =>
        new { results, _links = nextLink is null ? null : new { next = nextLink } };

    // ─── Authentication ──────────────────────────────────────────────────────

    [Fact]
    public async Task ConfigureHttpClient_BasicAuth_SetsAuthorizationHeader()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(JsonContent(PaginatedResult<ConfluenceSpace>([])));
        var sut = CreateSut(handler, CreateSettings());

        // Trigger an HTTP call so we can inspect the captured request
        try { await sut.GetSpaceByKeyAsync("TEST"); } catch { /* space not found is expected */ }

        var authHeader = handler.CapturedRequests[0].Headers.Authorization;
        authHeader.Should().NotBeNull();
        authHeader!.Scheme.Should().Be("Basic");

        // Decode and verify credential format
        var decoded = Encoding.ASCII.GetString(Convert.FromBase64String(authHeader.Parameter!));
        decoded.Should().Be("test@example.com:token");
    }

    [Fact]
    public async Task ConfigureHttpClient_BearerToken_SetsBearerHeader()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(JsonContent(PaginatedResult<ConfluenceSpace>([])));
        var settings = CreateSettings(authMode: "Bearer", bearerToken: "my-oauth-token");
        var sut = CreateSut(handler, settings);

        try { await sut.GetSpaceByKeyAsync("TEST"); } catch { /* expected */ }

        var authHeader = handler.CapturedRequests[0].Headers.Authorization;
        authHeader.Should().NotBeNull();
        authHeader!.Scheme.Should().Be("Bearer");
        authHeader.Parameter.Should().Be("my-oauth-token");
    }

    // ─── GetSpaceByKeyAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetSpaceByKeyAsync_ReturnsSpace()
    {
        var handler = new MockHttpMessageHandler();
        var space = new { id = "SP1", key = "DEV", name = "Development", homepageId = "100" };
        handler.Enqueue(JsonContent(PaginatedResult(new[] { space })));

        var sut = CreateSut(handler);
        var result = await sut.GetSpaceByKeyAsync("DEV");

        result.Id.Should().Be("SP1");
        result.Key.Should().Be("DEV");
        result.Name.Should().Be("Development");
    }

    [Fact]
    public async Task GetSpaceByKeyAsync_NotFound_Throws()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(JsonContent(PaginatedResult<object>([])));

        var sut = CreateSut(handler);

        await sut.Invoking(s => s.GetSpaceByKeyAsync("NOPE"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Space*'NOPE'*not found*");
    }

    // ─── GetPageByIdAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetPageByIdAsync_ReturnsPage()
    {
        var handler = new MockHttpMessageHandler();
        var page = new
        {
            id = "42",
            title = "My Page",
            spaceId = "SP1",
            parentId = (string?)null,
            body = new { storage = new { representation = "storage", value = "<p>Hello</p>" } },
            version = new { number = 3 }
        };
        handler.Enqueue(JsonContent(page));

        var sut = CreateSut(handler);
        var result = await sut.GetPageByIdAsync("42");

        result.Should().NotBeNull();
        result!.Id.Should().Be("42");
        result.Title.Should().Be("My Page");
        result.Body!.Storage!.Value.Should().Be("<p>Hello</p>");
        result.Version!.Number.Should().Be(3);
    }

    [Fact]
    public async Task GetPageByIdAsync_NotFound_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NotFound);

        var sut = CreateSut(handler);
        var result = await sut.GetPageByIdAsync("999");

        result.Should().BeNull();
    }

    // ─── GetPageByTitleAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetPageByTitleAsync_ReturnsFirstMatch()
    {
        var handler = new MockHttpMessageHandler();
        var page = new { id = "10", title = "Target", spaceId = "SP1", parentId = (string?)null, body = (object?)null, version = (object?)null };
        handler.Enqueue(JsonContent(PaginatedResult(new[] { page })));

        var sut = CreateSut(handler);
        var result = await sut.GetPageByTitleAsync("Target", "SP1");

        result.Should().NotBeNull();
        result!.Id.Should().Be("10");
    }

    [Fact]
    public async Task GetPageByTitleAsync_NoMatch_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(JsonContent(PaginatedResult<object>([])));

        var sut = CreateSut(handler);
        var result = await sut.GetPageByTitleAsync("Nonexistent", "SP1");

        result.Should().BeNull();
    }

    // ─── CreatePageAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePageAsync_PostsBody_ReturnsPage()
    {
        var handler = new MockHttpMessageHandler();
        var responseBody = new { id = "99", title = "New Page", spaceId = "SP1", parentId = "1", body = (object?)null, version = new { number = 1 } };
        handler.Enqueue(JsonContent(responseBody));

        var sut = CreateSut(handler);
        var result = await sut.CreatePageAsync("New Page", "<p>Content</p>", "1", "SP1");

        result.Id.Should().Be("99");
        result.Title.Should().Be("New Page");

        handler.CapturedRequests[0].Method.Should().Be(HttpMethod.Post);
        handler.CapturedRequests[0].RequestUri!.PathAndQuery.Should().Contain("/pages");
    }

    // ─── UpdatePageAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePageAsync_PutsBody_ReturnsPage()
    {
        var handler = new MockHttpMessageHandler();
        var responseBody = new { id = "42", title = "Updated", spaceId = "SP1", parentId = (string?)null, body = (object?)null, version = new { number = 5 } };
        handler.Enqueue(JsonContent(responseBody));

        var sut = CreateSut(handler);
        var result = await sut.UpdatePageAsync("42", "Updated", "<p>New</p>", 5);

        result.Id.Should().Be("42");
        result.Version!.Number.Should().Be(5);

        handler.CapturedRequests[0].Method.Should().Be(HttpMethod.Put);
        handler.CapturedRequests[0].RequestUri!.PathAndQuery.Should().Contain("/pages/42");
    }

    // ─── GetOrCreatePageUnderParentAsync ──────────────────────────────────────

    [Fact]
    public async Task GetOrCreatePageUnderParent_ExistingPage_ReturnsFoundUnderParent()
    {
        var handler = new MockHttpMessageHandler();
        // GetPagesByTitleAsync returns one match under the expected parent
        var page = new { id = "50", title = "Root", spaceId = "SP1", parentId = "1", body = (object?)null, version = (object?)null };
        handler.Enqueue(JsonContent(PaginatedResult(new[] { page })));

        var sut = CreateSut(handler);
        var resolution = await sut.GetOrCreatePageUnderParentAsync("Root", "1", "SP1");

        resolution.Status.Should().Be(ConfluenceRootPageResolutionStatus.FoundUnderParent);
        resolution.Page.Should().NotBeNull();
        resolution.Page!.Id.Should().Be("50");
    }

    [Fact]
    public async Task GetOrCreatePageUnderParent_NoMatch_CreatesPage()
    {
        var handler = new MockHttpMessageHandler();
        // GetPagesByTitleAsync returns empty (no matches)
        handler.Enqueue(JsonContent(PaginatedResult<object>([])));
        // CreatePageAsync response
        var created = new { id = "77", title = "New Root", spaceId = "SP1", parentId = "1", body = (object?)null, version = new { number = 1 } };
        handler.Enqueue(JsonContent(created));

        var sut = CreateSut(handler);
        var resolution = await sut.GetOrCreatePageUnderParentAsync("New Root", "1", "SP1");

        resolution.Status.Should().Be(ConfluenceRootPageResolutionStatus.Created);
        resolution.Page!.Id.Should().Be("77");
    }

    [Fact]
    public async Task GetOrCreatePageUnderParent_MultipleMatches_ReturnsAmbiguous()
    {
        var handler = new MockHttpMessageHandler();
        // Two pages with same title under different parents → ambiguous
        var pages = new[]
        {
            new { id = "10", title = "Dup", spaceId = "SP1", parentId = "A", body = (object?)null, version = (object?)null },
            new { id = "20", title = "Dup", spaceId = "SP1", parentId = "B", body = (object?)null, version = (object?)null }
        };
        handler.Enqueue(JsonContent(PaginatedResult(pages)));

        var sut = CreateSut(handler);
        var resolution = await sut.GetOrCreatePageUnderParentAsync("Dup", "C", "SP1");

        resolution.Status.Should().Be(ConfluenceRootPageResolutionStatus.Ambiguous);
        resolution.Page.Should().BeNull();
        resolution.TotalMatches.Should().Be(2);
    }

    // ─── GetOrCreatePageAsync (wrapper) ──────────────────────────────────────

    [Fact]
    public async Task GetOrCreatePageAsync_Ambiguous_ThrowsInvalidOperation()
    {
        var handler = new MockHttpMessageHandler();
        var pages = new[]
        {
            new { id = "10", title = "Dup", spaceId = "SP1", parentId = "A", body = (object?)null, version = (object?)null },
            new { id = "20", title = "Dup", spaceId = "SP1", parentId = "B", body = (object?)null, version = (object?)null }
        };
        handler.Enqueue(JsonContent(PaginatedResult(pages)));

        var sut = CreateSut(handler);

        await sut.Invoking(s => s.GetOrCreatePageAsync("Dup", "C", "SP1"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Ambiguous*");
    }

    // ─── FetchPaginatedAsync (via GetChildPagesAsync) ────────────────────────

    [Fact]
    public async Task GetChildPagesAsync_FollowsNextLink()
    {
        var handler = new MockHttpMessageHandler();

        // Page 1 of results with a next link
        var page1 = new { id = "10", title = "Child 1", spaceId = "SP1", parentId = "1", body = (object?)null, version = (object?)null };
        handler.Enqueue(JsonContent(PaginatedResult(new[] { page1 }, "/wiki/api/v2/pages/1/children?cursor=abc")));

        // Page 2 of results (no next link)
        var page2 = new { id = "20", title = "Child 2", spaceId = "SP1", parentId = "1", body = (object?)null, version = (object?)null };
        handler.Enqueue(JsonContent(PaginatedResult(new[] { page2 })));

        var sut = CreateSut(handler);
        var pages = new List<ConfluencePage>();
        await foreach (var p in sut.GetChildPagesAsync("1"))
            pages.Add(p);

        pages.Should().HaveCount(2);
        pages[0].Title.Should().Be("Child 1");
        pages[1].Title.Should().Be("Child 2");
        handler.CapturedRequests.Should().HaveCount(2); // Two HTTP calls
    }

    // ─── UploadAttachmentAsync ───────────────────────────────────────────────

    [Fact]
    public async Task UploadAttachmentAsync_NewAttachment_SendsMultipart()
    {
        var handler = new MockHttpMessageHandler();
        // GetAttachmentsAsync: empty list (no existing attachment)
        handler.Enqueue(JsonContent(PaginatedResult<object>([])));
        // Upload response: OK
        handler.Enqueue(HttpStatusCode.OK);

        var sut = CreateSut(handler);
        using var stream = new MemoryStream("test content"u8.ToArray());
        await sut.UploadAttachmentAsync("42", "image.png", stream, "image/png");

        // Second request should be the upload
        var uploadReq = handler.CapturedRequests[1];
        uploadReq.Method.Should().Be(HttpMethod.Post);
        uploadReq.RequestUri!.PathAndQuery.Should().Contain("/child/attachment");
        uploadReq.Headers.Should().Contain(h => h.Key == "X-Atlassian-Token");
    }

    // ─── Error handling ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_500_ThrowsHttpRequestException()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.InternalServerError);

        var sut = CreateSut(handler);

        await sut.Invoking(s => s.GetSpaceByKeyAsync("TEST"))
            .Should().ThrowAsync<HttpRequestException>();
    }

    // ─── GetPageLabelsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetPageLabelsAsync_ReturnsList()
    {
        var handler = new MockHttpMessageHandler();
        var labels = new[]
        {
            new { id = "1", name = "my-label", prefix = "global" },
            new { id = "2", name = "another", prefix = "global" }
        };
        handler.Enqueue(JsonContent(PaginatedResult(labels)));

        var sut = CreateSut(handler);
        var result = await sut.GetPageLabelsAsync("42");

        result.Should().HaveCount(2);
        result[0].Should().Be("my-label");
        result[1].Should().Be("another");
    }

    // ─── AddPageLabelsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task AddPageLabelsAsync_PostsLabels()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK);

        var sut = CreateSut(handler);
        await sut.AddPageLabelsAsync("42", ["tag1", "tag2"]);

        handler.CapturedRequests[0].Method.Should().Be(HttpMethod.Post);
        handler.CapturedRequests[0].RequestUri!.PathAndQuery.Should().Contain("/label");
    }

    [Fact]
    public async Task AddPageLabelsAsync_EmptyList_NoRequest()
    {
        var handler = new MockHttpMessageHandler();

        var sut = CreateSut(handler);
        await sut.AddPageLabelsAsync("42", []);

        handler.CapturedRequests.Should().BeEmpty();
    }

    // ─── DownloadAttachmentAsync (preserved from original) ───────────────────

    [Fact]
    public async Task DownloadAttachmentAsync_Should_UseAbsoluteUrl_AsIs()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK);

        var sut = CreateSut(handler);
        var absoluteUrl = "https://cdn.example.com/wiki/download/attachments/123/file.png";
        await using var stream = await sut.DownloadAttachmentAsync(absoluteUrl);

        handler.CapturedRequests[0].RequestUri.Should().Be(new Uri(absoluteUrl));
    }

    [Fact]
    public async Task DownloadAttachmentAsync_Should_PrefixRelativePath_WithApiBase()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK);

        var sut = CreateSut(handler);
        await using var stream = await sut.DownloadAttachmentAsync("/download/attachments/123/file.png");

        handler.CapturedRequests[0].RequestUri.Should().Be(
            new Uri("https://example.atlassian.net/wiki/download/attachments/123/file.png"));
    }

    [Fact]
    public async Task DownloadAttachmentAsync_Should_NotDoublePrefix_When_AlreadyApiBased()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK);

        var sut = CreateSut(handler);
        await using var stream = await sut.DownloadAttachmentAsync("/wiki/download/attachments/123/file.png");

        handler.CapturedRequests[0].RequestUri.Should().Be(
            new Uri("https://example.atlassian.net/wiki/download/attachments/123/file.png"));
    }

    // ─── SetContentPropertyAsync ─────────────────────────────────────────────

    [Fact]
    public async Task SetContentPropertyAsync_NewProperty_PostsSuccessfully()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK);

        var sut = CreateSut(handler);
        await sut.SetContentPropertyAsync("42", "my-key", "my-value");

        handler.CapturedRequests[0].Method.Should().Be(HttpMethod.Post);
        handler.CapturedRequests[0].RequestUri!.PathAndQuery.Should().Contain("/properties");
    }

    [Fact]
    public async Task SetContentPropertyAsync_Conflict_TriesToUpdate()
    {
        var handler = new MockHttpMessageHandler();
        // First POST returns 409 Conflict
        handler.Enqueue(HttpStatusCode.Conflict);
        // GET existing property
        var existingProp = new
        {
            results = new[]
            {
                new { id = "prop1", key = "my-key", value = "old-value", version = new { number = 2 } }
            }
        };
        handler.Enqueue(JsonContent(existingProp));
        // PUT update returns OK
        handler.Enqueue(HttpStatusCode.OK);

        var sut = CreateSut(handler);
        await sut.SetContentPropertyAsync("42", "my-key", "new-value");

        handler.CapturedRequests.Should().HaveCount(3); // POST (409) + GET + PUT
        handler.CapturedRequests[2].Method.Should().Be(HttpMethod.Put);
        handler.CapturedRequests[2].RequestUri!.PathAndQuery.Should().Contain("/properties/prop1");
    }

    // ─── Custom Headers ──────────────────────────────────────────────────────

    [Fact]
    public async Task ConfigureHttpClient_CustomHeaders_AreSent()
    {
        var handler = new MockHttpMessageHandler();
        handler.Enqueue(JsonContent(PaginatedResult<object>([])));

        var settings = CreateSettings();
        settings.CustomHeaders["X-Custom"] = "custom-value";
        var sut = CreateSut(handler, settings);

        try { await sut.GetSpaceByKeyAsync("TEST"); } catch { /* expected */ }

        handler.CapturedRequests[0].Headers.Should().Contain(h =>
            h.Key == "X-Custom" && h.Value.Contains("custom-value"));
    }

    // ─── Mock Handler ────────────────────────────────────────────────────────

    /// <summary>
    /// A test <see cref="HttpMessageHandler"/> that dequeues pre-configured responses
    /// and captures every request for later assertion.
    /// </summary>
    internal sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();

        /// <summary>All HTTP requests that were sent through this handler, in order.</summary>
        public List<HttpRequestMessage> CapturedRequests { get; } = [];

        /// <summary>Enqueue a response with JSON content.</summary>
        public void Enqueue(HttpContent content, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var response = new HttpResponseMessage(statusCode) { Content = content };
            _responses.Enqueue(response);
        }

        /// <summary>Enqueue a minimal response with the given status code.</summary>
        public void Enqueue(HttpStatusCode statusCode)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new ByteArrayContent("ok"u8.ToArray())
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            _responses.Enqueue(response);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequests.Add(request);

            if (_responses.Count == 0)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("No more queued responses")
                });
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
