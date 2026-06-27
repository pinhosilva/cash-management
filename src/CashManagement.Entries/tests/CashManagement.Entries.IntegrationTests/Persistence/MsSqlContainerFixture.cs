using Testcontainers.MsSql;
using Xunit;

namespace CashManagement.Entries.IntegrationTests.Persistence;

/// <summary>
/// Sobe um SQL Server real em container (Testcontainers), compartilhado pela
/// classe de teste. Expõe a connection string para o DbContext.
/// </summary>
public sealed class MsSqlContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
