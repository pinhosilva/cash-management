using CashManagement.Entries.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashManagement.Entries.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento da Transactional Outbox. O índice em ProcessedAt apoia a
/// varredura do relay pelas mensagens não-publicadas (T07).</summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("Outbox");
        builder.HasKey(o => o.Id);
        builder.HasIndex(o => o.ProcessedAt);
        builder.Property(o => o.Type).HasMaxLength(200);
    }
}
