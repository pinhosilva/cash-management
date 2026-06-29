using Microsoft.Extensions.Logging;

namespace CashManagement.Balance.Application.Features.BalanceProjection;

/// <summary>
/// Aplica um lançamento (crédito ou débito) à projeção do saldo diário. Não conhece
/// Kafka nem Mongo — só a porta <see cref="IDailyBalanceProjection"/>. A idempotência
/// (no-op ao reprocessar a mesma <c>event.id</c>) é garantida pela projeção.
/// </summary>
public sealed class EntryPostedEventHandler
{
    private readonly IDailyBalanceProjection _projection;
    private readonly ILogger<EntryPostedEventHandler> _logger;

    public EntryPostedEventHandler(IDailyBalanceProjection projection, ILogger<EntryPostedEventHandler> logger)
    {
        _projection = projection;
        _logger = logger;
    }

    public async Task HandleAsync(EntryPosted entry, CancellationToken cancellationToken = default)
    {
        var date = DateOnly.FromDateTime(entry.OccurredAt.ToUniversalTime());

        var applied = await _projection.ApplyAsync(entry.EventId, date, entry.Amount, entry.Kind, cancellationToken);

        // §8.2: sem PII/segredo — não logamos o valor em Info; só o tipo, o event.id, o
        // dia e o desfecho (aplicado/duplicado), sob o correlationId propagado do evento.
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["component"] = LogComponent.Projection,
            ["correlationId"] = entry.CorrelationId,
        }))
        {
            if (applied)
            {
                _logger.LogInformation(
                    "Lançamento {Kind} aplicado à projeção do dia {BalanceDate} (eventId={EventId}).",
                    entry.Kind, date, entry.EventId);
            }
            else
            {
                _logger.LogInformation(
                    "Evento já aplicado, ignorado (no-op) para o dia {BalanceDate} (eventId={EventId}).",
                    date, entry.EventId);
            }
        }
    }
}
