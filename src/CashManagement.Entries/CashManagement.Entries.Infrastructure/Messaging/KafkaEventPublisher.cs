using CashManagement.Entries.Application.Interfaces;
using Confluent.Kafka;

namespace CashManagement.Entries.Infrastructure.Messaging;

/// <summary>
/// Publica no Kafka via Confluent.Kafka. A <c>key</c> da mensagem é o
/// <c>aggregate.id</c> (define a partição → ordem por agregado, §4.3); o
/// <c>payload</c> é o envelope §4.3 já serializado.
/// </summary>
public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    public const string Topic = "cash.management.entries.events";

    private readonly IProducer<string, string> _producer;

    public KafkaEventPublisher(string bootstrapServers) =>
        _producer = new ProducerBuilder<string, string>(
            new ProducerConfig { BootstrapServers = bootstrapServers }).Build();

    public async Task PublishAsync(string key, string payload, CancellationToken cancellationToken = default) =>
        await _producer.ProduceAsync(
            Topic,
            new Message<string, string> { Key = key, Value = payload },
            cancellationToken);

    public void Dispose() => _producer.Dispose();
}
