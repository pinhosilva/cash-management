using CashManagement.Entries.Api.Auth;
using CashManagement.Entries.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace CashManagement.Entries.IntegrationTests.Api;

/// <summary>
/// Sobe a API real (<see cref="WebApplicationFactory{TEntryPoint}"/>) contra um
/// SQL Server em container, com a chave de assinatura JWT de dev. Compartilhada
/// pela classe de teste; expõe um <c>HttpClient</c> e o emissor de token de dev.
/// A chave/ambiente são configurados de forma <b>escopada</b> à factory (via
/// <c>UseSetting</c>/<c>UseEnvironment</c>), sem variável de ambiente global.
/// </summary>
public sealed class EntriesApiFixture : IAsyncLifetime
{
    private const string SigningKey = "integration-test-signing-key-32-bytes-min!!";

    // CU fixa (não `2022-latest`): o build mais novo crasha no runner do GitHub
    // ("CoInitializeSecurity failure"). Pinar = reproduzível e estável.
    private readonly MsSqlContainer _sql =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU12-ubuntu-22.04").Build();
    private WebApplicationFactory<Program> _factory = default!;

    public HttpClient Client { get; private set; } = default!;

    public string IssueWriteToken()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<DevTokenService>().IssueWriteToken();
    }

    /// <summary>Token autenticado, porém <b>sem</b> o scope <c>entries:write</c> — exercita o 403.</summary>
    public string IssueTokenWithoutWriteScope()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<DevTokenService>().IssueToken("dev-merchant", "balances:read");
    }

    /// <summary>Conta os eventos persistidos no event store para um agregado — confere a invariante de "um único evento".</summary>
    public async Task<int> CountStoredEventsAsync(Guid aggregateId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EntriesDbContext>();
        return await db.Events.CountAsync(e => e.AggregateId == aggregateId);
    }

    /// <summary>Tipo do evento gravado no event store para um agregado (ex.: <c>CreditPostedEvent</c>/<c>DebitPostedEvent</c>).</summary>
    public async Task<string?> StoredEventTypeAsync(Guid aggregateId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EntriesDbContext>();
        return await db.Events
            .Where(e => e.AggregateId == aggregateId)
            .Select(e => e.Type)
            .FirstOrDefaultAsync();
    }

    /// <summary>Payload do envelope §4.3 gravado na outbox para um agregado — confere o contrato publicado.</summary>
    public async Task<string?> OutboxPayloadAsync(Guid aggregateId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EntriesDbContext>();
        return await db.Outbox
            .Where(m => m.AggregateId == aggregateId)
            .Select(m => m.Payload)
            .FirstOrDefaultAsync();
    }

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.UseEnvironment("Development");
                host.UseSetting("ConnectionStrings:Entries", _sql.GetConnectionString());
                host.UseSetting("Kafka:BootstrapServers", "localhost:59092"); // relay falha em silêncio; não usado nestes testes
                host.UseSetting("Jwt:SigningKey", SigningKey);
            });

        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();
        await _sql.DisposeAsync();
    }
}
