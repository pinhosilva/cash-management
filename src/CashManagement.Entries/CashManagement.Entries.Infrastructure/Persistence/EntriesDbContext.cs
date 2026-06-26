using Microsoft.EntityFrameworkCore;

namespace CashManagement.Entries.Infrastructure.Persistence;

/// <summary>
/// Contexto EF Core do serviço de Lançamentos: o event store (<see cref="Events"/>)
/// e a Transactional Outbox (<see cref="Outbox"/>).
/// </summary>
public sealed class EntriesDbContext : DbContext
{
    public EntriesDbContext(DbContextOptions<EntriesDbContext> options) : base(options)
    {
    }

    public DbSet<StoredEvent> Events => Set<StoredEvent>();

    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredEvent>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(e => e.EventId);
            // append-only ordenado + concorrência otimista: um evento por (stream, versão)
            entity.HasIndex(e => new { e.AggregateId, e.Version }).IsUnique();
            entity.Property(e => e.Type).HasMaxLength(200);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("Outbox");
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.ProcessedAt); // o relay varre as não-processadas
            entity.Property(o => o.Type).HasMaxLength(200);
        });
    }
}
