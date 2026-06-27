using CashManagement.Entries.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace CashManagement.Entries.Infrastructure.Persistence;

/// <summary>
/// Contexto EF Core do serviço de Lançamentos: o event store (<see cref="Events"/>)
/// e a Transactional Outbox (<see cref="Outbox"/>). Os mapeamentos vivem em
/// <c>Configurations/</c> (IEntityTypeConfiguration), aplicados por varredura do assembly.
/// </summary>
public sealed class EntriesDbContext : DbContext
{
    public DbSet<StoredEvent> Events => Set<StoredEvent>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    public EntriesDbContext(DbContextOptions<EntriesDbContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EntriesDbContext).Assembly);
}
