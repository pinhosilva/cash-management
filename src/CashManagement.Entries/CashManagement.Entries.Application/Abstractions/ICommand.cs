namespace CashManagement.Entries.Application.Abstractions;

/// <summary>
/// Marca uma intenção de comando que, ao ser tratada, produz um
/// <typeparamref name="TResult"/>. Marker interface (sem membros) — o tipo do
/// resultado é o contrato.
/// </summary>
public interface ICommand<TResult>;
