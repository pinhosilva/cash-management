using Microsoft.Extensions.Logging;

namespace CashManagement.Balance.Application.Features.BalanceProjection;

/// <summary>
/// Aplica um <c>CreditPostedEvent</c> à projeção do saldo diário. Não conhece Kafka
/// nem Mongo — só a porta <see cref="IDailyBalanceProjection"/>. A idempotência
/// (no-op ao reprocessar a mesma <c>event.id</c>) é garantida pela projeção.
/// </summary>
public sealed class CreditPostedEventHandler
{
    private readonly IDailyBalanceProjection _projection;
    private readonly ILogger<CreditPostedEventHandler> _logger;

    public CreditPostedEventHandler(IDailyBalanceProjection projection, ILogger<CreditPostedEventHandler> logger)
    {
        _projection = projection;
        _logger = logger;
    }

    public async Task HandleAsync(CreditPosted credit, CancellationToken cancellationToken = default)
    {
        var date = DateOnly.FromDateTime(credit.OccurredAt.ToUniversalTime());

        var applied = await _projection.ApplyCreditAsync(credit.EventId, date, credit.Amount, cancellationToken);

        // §8.2: sem PII/segredo — não logamos o valor em Info; só o event.id, o dia e
        // o desfecho (aplicado/duplicado), sob o correlationId propagado do evento.
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["component"] = LogComponent.Projection,
            ["correlationId"] = credit.CorrelationId,
        }))
        {
            if (applied)
            {
                _logger.LogInformation(
                    "Crédito aplicado à projeção do dia {BalanceDate} (eventId={EventId}).", date, credit.EventId);
            }
            else
            {
                _logger.LogInformation(
                    "Evento já aplicado, ignorado (no-op) para o dia {BalanceDate} (eventId={EventId}).", date, credit.EventId);
            }
        }
    }
}
