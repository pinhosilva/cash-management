namespace CashManagement.Entries.Application.Interfaces;

/// <summary>
/// Expõe a <c>Idempotency-Key</c> da requisição corrente (§4.3) para o behavior de
/// idempotência, <b>sem</b> que a Application conheça HTTP. Preenchida na borda (uma
/// vez por requisição) e <c>null</c> quando o cliente não enviou a chave.
/// </summary>
public interface IIdempotencyContext
{
    string? Key { get; }
}
