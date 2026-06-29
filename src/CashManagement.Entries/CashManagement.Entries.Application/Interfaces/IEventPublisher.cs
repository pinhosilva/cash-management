namespace CashManagement.Entries.Application.Interfaces;

/// <summary>
/// Publica uma mensagem (key + payload) no broker. Transport-neutral — a
/// implementação (Kafka) conhece o tópico. Usado pelo relay da outbox (§5.9).
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(string key, string payload, CancellationToken cancellationToken = default);
}
