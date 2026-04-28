using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace GridMonitor.Tests.Unit.Shared;

public class InMemoryDb
{
	public static AppDbContext Create(string dbName = null)
	{
		var name = string.IsNullOrWhiteSpace(dbName) ? Guid.NewGuid().ToString() : dbName;
		var opts = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase(name)
			.Options;

		return new AppDbContext(opts);
	}
}
