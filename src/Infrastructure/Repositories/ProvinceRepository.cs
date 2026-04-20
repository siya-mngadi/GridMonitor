using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace GridMonitor.Infrastructure.Repositories;

public class ProvinceRepository : IProvinceRepository
{
	private readonly AppDbContext context;
	public ProvinceRepository(AppDbContext context)
	{
		this.context = context;
	}

	public IUnitOfWork UnitOfWork => context;

	public async ValueTask<List<Province>> GetAsync(CancellationToken ct = default)
	{
		return await context.Provinces.AsNoTracking().ToListAsync(ct);
	}

	public async ValueTask<Province> GetByIdAsync(int id, CancellationToken ct = default)
	{
		return await context.Provinces
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.Id == id, ct);
	}

	public async ValueTask<Province> GetByEskomIdAsync(int eskomId, CancellationToken ct = default)
	{
		return await context.Provinces
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.EskomId == eskomId, ct);
	}

	public async ValueTask<Province> GetByNameAsync(string name, CancellationToken ct = default)
	{
		return await context.Provinces
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.Name == name, ct);
	}

	public async ValueTask<Province> UpsertAsync(Province province, CancellationToken ct = default)
	{
		var existingProvince = await context.Provinces
			.FirstOrDefaultAsync(p => p.EskomId == province.EskomId, ct);

		if (existingProvince is null)
		{
			await context.Provinces.AddAsync(province, ct);
		}
		else
		{
			context.Provinces.Update(province);
		}
		return province;
	}
}
