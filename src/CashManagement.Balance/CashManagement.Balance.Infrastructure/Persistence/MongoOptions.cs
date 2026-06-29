namespace CashManagement.Balance.Infrastructure.Persistence;

/// <summary>Configuração de conexão com o MongoDB do read model.</summary>
public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    public string Database { get; set; } = "cash_management_balance";

    public string DailyBalanceCollection { get; set; } = "daily_balances";
}
