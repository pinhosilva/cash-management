using CashManagement.Balance.Application.Common;

namespace CashManagement.Balance.Application.Features.GetDailyBalance;

/// <summary>
/// Handler da consulta do saldo diário: lê a projeção pela porta
/// <see cref="IDailyBalanceReader"/> e devolve um <see cref="Result{T}"/> — sucesso
/// com o <see cref="DailyBalanceDto"/>, ou falha <c>NotFound</c> quando não há
/// projeção para o dia (§4.4). Sem lógica de negócio: o saldo já vem consolidado.
/// </summary>
public sealed class GetDailyBalanceQueryHandler
{
    private readonly IDailyBalanceReader _reader;

    public GetDailyBalanceQueryHandler(IDailyBalanceReader reader) => _reader = reader;

    public async Task<Result<DailyBalanceDto>> HandleAsync(GetDailyBalanceQuery query, CancellationToken cancellationToken = default)
    {
        var balance = await _reader.GetAsync(query.Date, cancellationToken);

        return balance is null
            ? Result.Fail<DailyBalanceDto>(Error.NotFound("No balance found for the requested date."))
            : Result.Ok(balance);
    }
}
