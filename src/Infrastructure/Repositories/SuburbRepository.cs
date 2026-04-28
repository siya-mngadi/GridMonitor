using EFCore.BulkExtensions;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace GridMonitor.Infrastructure.Repositories;

public class SuburbRepository : ISuburbRepository
{
	private readonly AppDbContext context;

	public SuburbRepository(AppDbContext context)
	{
		this.context = context;
	}

	public IUnitOfWork UnitOfWork => context;

	public async ValueTask<IList<Suburb>> GetAsync(CancellationToken ct = default)
	{
		return await context.Suburbs
			.AsNoTracking()
			.Include(s => s.Municipality)
			.ToListAsync(ct);
	}

	public async ValueTask<Suburb> GetByEskomIdAsync(int eskomId, CancellationToken ct = default)
	{
		return await context.Suburbs
			.AsNoTracking()
			.Include(s => s.Municipality)
			.FirstOrDefaultAsync(s => s.EskomId == eskomId, ct);
	}

	public async ValueTask<Suburb> GetByIdAsync(int id, CancellationToken ct = default)
	{
		return await context.Suburbs
			.AsNoTracking()
			.Include(s => s.Municipality)
			.Include(s => s.Slots)
			.AsSplitQuery()
			.FirstOrDefaultAsync(s => s.Id == id, ct);
	}

	public async ValueTask<IList<Suburb>> GetByMunicipalityAsync(int municipalityId, CancellationToken ct = default)
	{
		return await context.Suburbs
			.AsNoTracking()
			.Include(s => s.Municipality)
			.Where(s => s.MunicipalityId == municipalityId)
			.OrderBy(s => s.Name)
			.ToListAsync(ct);
	}

	public async ValueTask<IList<Suburb>> SearchAsync(string searchPhrase, int limit, CancellationToken ct = default)
	{
		return await context.Suburbs
			.AsNoTracking()
			.Include(s => s.Municipality)
			.Where(s => EF.Functions.Like(s.Name, $"%{searchPhrase}%") || EF.Functions.Like(s.Municipality.Name, $"%{searchPhrase}%"))
			.Take(limit)
			.OrderBy(s => s.Name)
			.ToListAsync(ct);
	}

	public async ValueTask<int> UpsertAsync(IEnumerable<Suburb> suburbs, CancellationToken ct = default)
	{
		var config = new BulkConfig
		{
			CalculateStats = true,
			UpdateByProperties = [nameof(Suburb.EskomId)],
			PropertiesToIncludeOnUpdate = [
				nameof(Suburb.Name),
				nameof(Suburb.MunicipalityId),
				nameof(Suburb.Total),
				nameof(Suburb.LastSyncedAt)
			]
		};
		await context.BulkInsertOrUpdateAsync(suburbs, config, type: typeof(Suburb), cancellationToken: ct);
		return config.StatsInfo?.StatsNumberInserted + config.StatsInfo?.StatsNumberUpdated ?? 0;
	}
}
