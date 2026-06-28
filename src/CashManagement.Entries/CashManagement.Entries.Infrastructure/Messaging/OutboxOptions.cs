namespace CashManagement.Entries.Infrastructure.Messaging;

/// <summary>Tuning do relay da outbox (§5.9). Vinculado da seção <c>Outbox</c> do appsettings.</summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>Intervalo de polling da outbox, em segundos.</summary>
    public int PollIntervalSeconds { get; set; } = 2;
}
