using CashManagement.Entries.Application.Interfaces;
using CashManagement.Entries.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashManagement.Entries.Infrastructure.Messaging;

/// <summary>
/// Núcleo do relay (Transactional Outbox, §5.9): varre as mensagens não
/// publicadas em ordem, publica cada uma e marca como processada. Publica
/// <b>antes</b> de marcar — se cair entre publish e marca, republica (at-least-once;
/// o consumidor deduplica por <c>event.id</c>, T09). Em falha de publish, **para**
/// e deixa a mensagem (e as seguintes) para o próximo ciclo — preservando a ordem.
/// </summary>
public sealed class OutboxProcessor
{
    private readonly EntriesDbContext _db;
    private readonly IEventPublisher _publisher;

    public OutboxProcessor(EntriesDbContext db, IEventPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _db.Outbox
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var message in pending)
        {
            try
            {
                await _publisher.PublishAsync(message.AggregateId.ToString(), message.Payload, cancellationToken);
            }
            catch
            {
                // deixa não-publicada; retry no próximo ciclo (ordem preservada). DLQ é evolução (§11/FAQ#9).
                break;
            }

            message.ProcessedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
