using Testcontainers.MongoDb;

namespace CashManagement.Balance.IntegrationTests;

/// <summary>Sobe um MongoDB real em container (Testcontainers), compartilhado pela classe de teste.</summary>
public sealed class MongoContainerFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container =
        new MongoDbBuilder("mongo:7.0").Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
