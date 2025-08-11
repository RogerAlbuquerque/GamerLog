using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace GameLog.Application.Abstractions;

/// <summary>
/// The slice of the EF Core context the application layer needs. EF Core already is a
/// unit of work and repository, so services query <c>Set&lt;T&gt;()</c> directly.
/// </summary>
public interface IAppDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    DatabaseFacade Database { get; }
}
