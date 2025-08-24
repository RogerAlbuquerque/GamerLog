using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace GameLog.Application.Abstractions;

/// <summary>
/// The slice of the EF Core context the application layer needs. EF Core already is a
/// unit of work and repository, so services query it directly.
/// </summary>
public interface IAppDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    DatabaseFacade Database { get; }

    /// <summary>
    /// Tracked query. Typed as <see cref="IQueryable{T}"/> on purpose: <c>DbSet&lt;T&gt;</c> also implements
    /// <c>IAsyncEnumerable&lt;T&gt;</c>, which makes async LINQ calls ambiguous on .NET 10 when applied to it directly.
    /// Use this (or <see cref="ReadOnly{TEntity}"/>) for reads and <c>Set&lt;T&gt;()</c> for Add/Remove.
    /// </summary>
    IQueryable<TEntity> Query<TEntity>() where TEntity : class => Set<TEntity>();

    /// <summary>No-tracking query for read-only paths.</summary>
    IQueryable<TEntity> ReadOnly<TEntity>() where TEntity : class => Set<TEntity>().AsNoTracking();
}
