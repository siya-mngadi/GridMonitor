
using EFCore.BulkExtensions;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace GridMonitor.Infrastructure.Repositories;

public class MunicipalityRepository : IMunicipalityRepository
{
	private readonly AppDbContext context;

	public MunicipalityRepository(AppDbContext context)
	{
		this.context = context;
	}

	public IUnitOfWork UnitOfWork => context;

	public async ValueTask<Municipality> GetByEskomId(int eskomId, CancellationToken ct = default)
	{
		return await context.Municipalities
			.AsNoTracking()
			.FirstOrDefaultAsync(m => m.EskomId == eskomId, ct);
	}

	public async ValueTask<Municipality> GetById(int id, CancellationToken ct = default)
	{
		return await context.Municipalities
			.AsNoTracking()
			.FirstOrDefaultAsync(m => m.Id == id, ct);
	}

	public async ValueTask<List<Municipality>> GetByProvinceAsync(int provinceId, CancellationToken ct = default)
	{
		return await context.Municipalities
			.AsNoTracking()
			.Where(m => m.ProvinceId == provinceId	)
			.ToListAsync(ct);
	}

	public async ValueTask<List<Municipality>> GetBySearchPhrase(string searchPhrase, int limit, CancellationToken ct = default)
	{
		return await context.Municipalities
			.AsNoTracking()
			.Where(m => EF.Functions.ILike(m.Name, $"%{searchPhrase}%"))
			.Take(limit)
			.ToListAsync(ct);
	}

	public async ValueTask<int> UpsertAsync(IEnumerable<Municipality> municipalities, CancellationToken ct = default)
	{
		var config = new BulkConfig
		{
			CalculateStats = true,
			UpdateByProperties = [nameof(Municipality.EskomId)],
			PropertiesToIncludeOnUpdate =
			[
				nameof(Municipality.Name),
				nameof(Municipality.ProvinceId),
				nameof(Municipality.Total),
				nameof(Municipality.LastSyncedAt)
			]
		};
		await context.BulkInsertOrUpdateAsync(municipalities, config, cancellationToken:ct);

		return config.StatsInfo?.StatsNumberInserted + config.StatsInfo?.StatsNumberInserted ?? 0;
	}
}
