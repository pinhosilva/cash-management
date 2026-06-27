using CashManagement.Entries.Api.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace CashManagement.Entries.IntegrationTests.Api;

/// <summary>
/// Sobe a API real (<see cref="WebApplicationFactory{TEntryPoint}"/>) contra um
/// SQL Server em container, com a chave de assinatura JWT de dev. Compartilhada
/// pela classe de teste; expõe um <c>HttpClient</c> e o emissor de token de dev.
/// </summary>
public sealed class EntriesApiFixture : IAsyncLifetime
{
    private const string SigningKey = "integration-test-signing-key-32-bytes-min!!";

    private readonly MsSqlContainer _sql =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    private WebApplicationFactory<Program> _factory = default!;

    public HttpClient Client { get; private set; } = default!;

    public string IssueWriteToken()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<DevTokenService>().IssueWriteToken();
    }

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();

        Environment.SetEnvironmentVariable("ENTRIES_JWT_SIGNING_KEY", SigningKey);

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.UseSetting("ConnectionStrings:Entries", _sql.GetConnectionString());
                host.UseSetting("Kafka:BootstrapServers", "localhost:59092"); // relay falha em silêncio; não usado nestes testes
            });

        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();
        await _sql.DisposeAsync();
    }
}
