using CashManagement.Entries.Application.Interfaces;

namespace CashManagement.Entries.Infrastructure.Persistence;

/// <summary>Implementação simples do <see cref="IIdGenerator"/> sobre <c>Guid.NewGuid()</c>.</summary>
public sealed class GuidIdGenerator : IIdGenerator
{
    public Guid New() => Guid.NewGuid();
}
