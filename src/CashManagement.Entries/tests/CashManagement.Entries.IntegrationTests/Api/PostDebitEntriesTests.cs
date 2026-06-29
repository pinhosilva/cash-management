using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using Xunit;

namespace CashManagement.Entries.IntegrationTests.Api;

/// <summary>
/// Integração do <c>POST /entries</c> com <c>type=Debit</c>: contrato de sucesso
/// (§4.4), gravação do <c>DebitPostedEvent</c> no event store e o envelope §4.3 na
/// outbox com <c>event.type = "DebitPostedEvent"</c>. O crédito permanece o default
/// (corpo sem <c>type</c>), exercitado nos demais testes desta fatia.
/// </summary>
public class PostDebitEntriesTests : IClassFixture<EntriesApiFixture>
{
    private readonly EntriesApiFixture _fixture;

    public PostDebitEntriesTests(EntriesApiFixture fixture) => _fixture = fixture;

    private HttpRequestMessage AuthorizedPost(object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/entries")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.IssueWriteToken());
        return request;
    }

    [Fact]
    public async Task Posting_a_debit_returns_201_and_stores_a_DebitPostedEvent()
    {
        var request = AuthorizedPost(new { type = "Debit", amount = 40m, occurredAt = DateTime.UtcNow });

        var response = await _fixture.Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var root = await ReadJsonAsync(response);
        root.GetProperty("status").GetString().ShouldBe("success");
        var id = root.GetProperty("result").GetProperty("id").GetString();
        id.ShouldNotBeNullOrWhiteSpace();

        var aggregateId = Guid.Parse(id!);
        (await _fixture.StoredEventTypeAsync(aggregateId)).ShouldBe("DebitPostedEvent");
    }

    [Fact]
    public async Task The_outbox_envelope_carries_the_debit_event_type_and_amount()
    {
        var request = AuthorizedPost(new { type = "Debit", amount = 30m, occurredAt = DateTime.UtcNow });

        var response = await _fixture.Client.SendAsync(request);
        var id = (await ReadJsonAsync(response)).GetProperty("result").GetProperty("id").GetString();

        var payload = await _fixture.OutboxPayloadAsync(Guid.Parse(id!));
        payload.ShouldNotBeNull();

        using var doc = JsonDocument.Parse(payload!);
        var envelope = doc.RootElement;
        envelope.GetProperty("event").GetProperty("type").GetString().ShouldBe("DebitPostedEvent");
        envelope.GetProperty("data").GetProperty("amount").GetProperty("amount").GetDecimal().ShouldBe(30m);
        envelope.GetProperty("data").GetProperty("amount").GetProperty("currency").GetString().ShouldBe("BRL");
    }

    [Fact]
    public async Task Posting_without_a_type_defaults_to_credit()
    {
        var request = AuthorizedPost(new { amount = 25m, occurredAt = DateTime.UtcNow });

        var response = await _fixture.Client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var id = (await ReadJsonAsync(response)).GetProperty("result").GetProperty("id").GetString();
        (await _fixture.StoredEventTypeAsync(Guid.Parse(id!))).ShouldBe("CreditPostedEvent");
    }

    [Fact]
    public async Task Posting_an_unknown_type_returns_400_with_validation_envelope()
    {
        var request = AuthorizedPost(new { type = "Reversal", amount = 10m, occurredAt = DateTime.UtcNow });

        var response = await _fixture.Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var root = await ReadJsonAsync(response);
        root.GetProperty("status").GetString().ShouldBe("error");
        root.GetProperty("error").GetProperty("code").GetString().ShouldBe("VALIDATION_FAILED");
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        return doc.RootElement.Clone();
    }
}
