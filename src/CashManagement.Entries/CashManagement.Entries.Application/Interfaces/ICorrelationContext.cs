namespace CashManagement.Entries.Application.Interfaces;

/// <summary>
/// Contexto da requisição corrente (escopo): o <c>correlationId</c> (§4.3) e o
/// ator (<c>initiatedBy</c>, identidade do JWT). Preenchido na borda da API e
/// lido onde o envelope da outbox é montado, para propagar a rastreabilidade
/// ponta-a-ponta sem acoplar a Infra ao HTTP.
/// </summary>
public interface ICorrelationContext
{
    string CorrelationId { get; }

    string? InitiatedBy { get; }
}
