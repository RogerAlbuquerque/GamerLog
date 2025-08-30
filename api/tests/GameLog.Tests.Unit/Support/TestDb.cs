using GameLog.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Tests.Unit.Support;

/// <summary>
/// A throw-away relational database per test. The in-memory SQLite connection is kept open for
/// the lifetime of the instance; every <see cref="Create"/> call returns a fresh context on it,
/// which mimics separate requests (nothing leaks through the change tracker).
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");

    public TestDb()
    {
        connection.Open();
        using var context = Create();
        context.Database.EnsureCreated();
    }

    public AppDbContext Create() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);

    public void Dispose() => connection.Dispose();
}
