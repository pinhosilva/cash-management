namespace CashManagement.Entries.Application.Interfaces;

/// <summary>
/// Gera o identificador do stream (aggregateId / partition key). Vive na
/// aplicação porque o id nasce antes de persistir (§5.10); a implementação
/// concreta fica na Infra/Api.
/// </summary>
public interface IIdGenerator
{
    Guid New();
}
