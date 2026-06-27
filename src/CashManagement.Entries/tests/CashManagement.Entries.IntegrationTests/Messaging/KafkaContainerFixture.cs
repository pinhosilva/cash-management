using Testcontainers.Kafka;
using Xunit;

namespace CashManagement.Entries.IntegrationTests.Messaging;

/// <summary>Sobe um broker Kafka real em container (Testcontainers), compartilhado pela classe de teste.</summary>
public sealed class KafkaContainerFixture : IAsyncLifetime
{
    private readonly KafkaContainer _container =
        new KafkaBuilder("confluentinc/cp-kafka:7.6.1").Build();

    public string BootstrapAddress => _container.GetBootstrapAddress();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
