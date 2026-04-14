using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GridMonitor.Infrastructure.DataContext;

public class AppDbContext : DbContext, IUnitOfWork
{
	public const string DefaultSchema = "dbo";
	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base(options)
	{
	}

	// Geography
	public DbSet<Province> Provinces => Set<Province>();
	public DbSet<Municipality> Municipalities => Set<Municipality>();
	public DbSet<Suburb> Suburbs => Set<Suburb>();

	// Schedule
	public DbSet<ScheduleSlot> ScheduleSlots => Set<ScheduleSlot>();
	public DbSet<StageSnapshot> StageSnapshots => Set<StageSnapshot>();

	// Users & alerts
	public DbSet<User> Users => Set<User>();
	public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
	public DbSet<AlertSubscription> Subscriptions => Set<AlertSubscription>();
	public DbSet<AlertChannel> Channels => Set<AlertChannel>();
	public DbSet<AlertLog> AlertLogs => Set<AlertLog>();

	// Sync audit
	public DbSet<SyncRun> SyncRuns => Set<SyncRun>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.HasDefaultSchema(DefaultSchema);
		// Picks up every IEntityTypeConfiguration<T> in this assembly automatically.
		// Add a new schema definition class and it's included — no changes needed here.
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

		modelBuilder.Entity<Province>().HasData(
			new Province { Id = 1, EskomId = 1, Name = "Eastern Cape" },
			new Province { Id = 2, EskomId = 2, Name = "Free State" },
			new Province { Id = 3, EskomId = 3, Name = "Gauteng" },
			new Province { Id = 4, EskomId = 4, Name = "KwaZulu-Natal" },
			new Province { Id = 5, EskomId = 5, Name = "Limpopo" },
			new Province { Id = 6, EskomId = 6, Name = "Mpumalanga" },
			new Province { Id = 7, EskomId = 7, Name = "North West" },
			new Province { Id = 8, EskomId = 8, Name = "Northern Cape" },
			new Province { Id = 9, EskomId = 9, Name = "Western Cape" }
		);

		base.OnModelCreating(modelBuilder);
	}

	public async ValueTask<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
	{
		var affected = await SaveChangesAsync(cancellationToken);
		return affected > 0;
	}
}
