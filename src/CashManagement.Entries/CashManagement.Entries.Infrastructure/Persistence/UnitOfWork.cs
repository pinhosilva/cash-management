using CashManagement.Entries.Domain.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CashManagement.Entries.Infrastructure.Persistence;

/// <summary>
/// Unit of Work sobre EF Core: um único <c>SaveChanges</c> persiste tudo que foi
/// encenado (eventos + outbox) atomicamente — o próprio <c>SaveChanges</c> já é
/// transacional. Violação do índice único (AggregateId, Version) vira
/// <see cref="ConcurrencyConflictException"/>.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private const int SqlUniqueViolation = 2627;
    private const int SqlUniqueIndexViolation = 2601;

    private readonly EntriesDbContext _db;

    public UnitOfWork(EntriesDbContext db) => _db = db;

    public async Task CommitAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConcurrencyConflictException(ex);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: SqlUniqueViolation or SqlUniqueIndexViolation };
}
