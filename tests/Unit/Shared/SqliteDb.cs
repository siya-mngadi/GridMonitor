using GridMonitor.Infrastructure.DataContext;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace GridMonitor.Tests.Unit.Shared;

public class SqliteDb : IDisposable
{
	private readonly DbConnection _connection;
	public AppDbContext Ctx { get; }

	public SqliteDb()
	{
		_connection = new SqliteConnection("DataSource=:memory:");
		_connection.Open();

		var opts = new DbContextOptionsBuilder<AppDbContext>()
			.UseSqlite(_connection)
			.Options;

		Ctx = new AppDbContext(opts);
		Ctx.Database.EnsureCreated();
	}

	public void Dispose()
	{
		Ctx.Dispose();
		_connection.Dispose();
	}
}
