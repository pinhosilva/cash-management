using CashManagement.Entries.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashManagement.Entries.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento do event store. O índice único (AggregateId, Version) garante
/// append-only ordenado + concorrência otimista (um evento por stream/versão).</summary>
public sealed class StoredEventConfiguration : IEntityTypeConfiguration<StoredEvent>
{
    public void Configure(EntityTypeBuilder<StoredEvent> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.EventId);
        builder.HasIndex(e => new { e.AggregateId, e.Version }).IsUnique();
        builder.Property(e => e.Type).HasMaxLength(200);
    }
}
