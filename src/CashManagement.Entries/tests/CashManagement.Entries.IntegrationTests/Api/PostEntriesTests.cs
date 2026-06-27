using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using Xunit;

namespace CashManagement.Entries.IntegrationTests.Api;

/// <summary>
/// Testes de integração do <c>POST /entries</c> (T08): contrato de sucesso/erro
/// (§4.4), autenticação JWT (§9.1) e idempotência de escrita (§4.3).
/// </summary>
public class PostEntriesTests : IClassFixture<EntriesApiFixture>
{
    private readonly EntriesApiFixture _fixture;

    public PostEntriesTests(EntriesApiFixture fixture) => _fixture = fixture;

    private HttpRequestMessage AuthorizedPost(object body, string? idempotencyKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/entries")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.IssueWriteToken());
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return request;
    }

    [Fact]
    public async Task Posting_a_credit_with_a_valid_token_returns_201_with_location_and_success_envelope()
    {
        var request = AuthorizedPost(new { amount = 100m, occurredAt = DateTime.UtcNow });

        var response = await _fixture.Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var root = await ReadJsonAsync(response);
        root.GetProperty("status").GetString().ShouldBe("success");
        var id = root.GetProperty("result").GetProperty("id").GetString();
        id.ShouldNotBeNullOrWhiteSpace();
        root.GetProperty("correlationId").GetString().ShouldNotBeNullOrWhiteSpace();

        response.Headers.Location!.ToString().ShouldBe($"/entries/{id}");
    }

    [Fact]
    public async Task Posting_without_a_token_returns_401_with_error_envelope()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/entries", new { amount = 100m });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var root = await ReadJsonAsync(response);
        root.GetProperty("status").GetString().ShouldBe("error");
        root.GetProperty("error").GetProperty("code").GetString().ShouldBe("UNAUTHORIZED");
        root.GetProperty("correlationId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Posting_with_a_token_missing_the_write_scope_returns_403_with_error_envelope()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/entries")
        {
            Content = JsonContent.Create(new { amount = 100m, occurredAt = DateTime.UtcNow }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.IssueTokenWithoutWriteScope());

        var response = await _fixture.Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var root = await ReadJsonAsync(response);
        root.GetProperty("status").GetString().ShouldBe("error");
        root.GetProperty("error").GetProperty("code").GetString().ShouldBe("INSUFFICIENT_SCOPE");
        root.GetProperty("correlationId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Posting_a_non_positive_amount_returns_400_with_validation_envelope()
    {
        var request = AuthorizedPost(new { amount = 0m, occurredAt = DateTime.UtcNow });

        var response = await _fixture.Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var root = await ReadJsonAsync(response);
        root.GetProperty("status").GetString().ShouldBe("error");
        root.GetProperty("error").GetProperty("code").GetString().ShouldBe("VALIDATION_FAILED");
        root.TryGetProperty("error", out var error).ShouldBeTrue();
        error.TryGetProperty("details", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Echoes_the_provided_correlation_id()
    {
        var correlationId = Guid.NewGuid().ToString();
        var request = AuthorizedPost(new { amount = 42m, occurredAt = DateTime.UtcNow });
        request.Headers.Add("X-Correlation-Id", correlationId);

        var response = await _fixture.Client.SendAsync(request);

        response.Headers.GetValues("X-Correlation-Id").ShouldContain(correlationId);
        var root = await ReadJsonAsync(response);
        root.GetProperty("correlationId").GetString().ShouldBe(correlationId);
    }

    [Fact]
    public async Task Generates_a_correlation_id_when_absent_and_echoes_it_in_the_response()
    {
        var request = AuthorizedPost(new { amount = 10m, occurredAt = DateTime.UtcNow });

        var response = await _fixture.Client.SendAsync(request);

        var header = response.Headers.GetValues("X-Correlation-Id").Single();
        header.ShouldNotBeNullOrWhiteSpace();
        var root = await ReadJsonAsync(response);
        root.GetProperty("correlationId").GetString().ShouldBe(header);
    }

    [Fact]
    public async Task Repeating_the_same_idempotency_key_returns_the_original_id_and_creates_a_single_event()
    {
        var key = Guid.NewGuid().ToString();

        var first = await _fixture.Client.SendAsync(AuthorizedPost(new { amount = 77m, occurredAt = DateTime.UtcNow }, key));
        var firstId = (await ReadJsonAsync(first)).GetProperty("result").GetProperty("id").GetString();

        var second = await _fixture.Client.SendAsync(AuthorizedPost(new { amount = 999m, occurredAt = DateTime.UtcNow }, key));
        var secondId = (await ReadJsonAsync(second)).GetProperty("result").GetProperty("id").GetString();

        second.StatusCode.ShouldBe(HttpStatusCode.Created);
        secondId.ShouldBe(firstId);

        // O replay não gera um segundo evento no event store: dedup idempotente de escrita (§4.3).
        (await _fixture.CountStoredEventsAsync(Guid.Parse(firstId!))).ShouldBe(1);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        return doc.RootElement.Clone();
    }
}
