using Testcontainers.MsSql;
using Xunit;

namespace CashManagement.Entries.IntegrationTests.Persistence;

/// <summary>
/// Sobe um SQL Server real em container (Testcontainers), compartilhado pela
/// classe de teste. Expõe a connection string para o DbContext.
/// </summary>
public sealed class MsSqlContainerFixture : IAsyncLifetime
{
    // CU fixa (não `2022-latest`): o build mais novo do `latest` crasha no startup
    // em runners do GitHub Actions com "CoInitializeSecurity failure". Pinar também
    // é build reproduzível. Mantém o tools18, compatível com o wait do Testcontainers.
    private readonly MsSqlContainer _container =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU12-ubuntu-22.04").Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
