using CashManagement.Entries.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashManagement.Entries.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento da tabela de deduplicação de escrita (§4.3). A chave é o
/// próprio <c>Idempotency-Key</c>, garantindo unicidade da operação lógica.</summary>
public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyKeys");

        builder.HasKey(r => r.Key);
        builder.Property(r => r.Key).HasMaxLength(200);
    }
}
