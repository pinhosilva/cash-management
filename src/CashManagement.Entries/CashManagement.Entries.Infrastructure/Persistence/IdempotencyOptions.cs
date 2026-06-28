namespace CashManagement.Entries.Infrastructure.Persistence;

/// <summary>Tuning da idempotência de escrita (§4.3). Vinculado da seção <c>Idempotency</c> do appsettings.</summary>
public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    /// <summary>Janela de deduplicação, em horas: chaves mais antigas são tratadas como ausentes.</summary>
    public int WindowHours { get; set; } = 24;
}
