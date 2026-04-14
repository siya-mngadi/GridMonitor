using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;

namespace GridMonitor.Worker;

internal class GridService : IGridService
{
	private readonly IMunicipalityRepository municipalityRepository;
	private readonly ISuburbRepository suburbRepository;
	private readonly IProvinceRepository provinceRepository;
	private readonly ISyncRunRepository syncRunRepository;

	public GridService(
		IMunicipalityRepository municipalityRepository,
		ISuburbRepository suburbRepository,
		IProvinceRepository provinceRepository,
		ISyncRunRepository syncRunRepository)
	{
		this.municipalityRepository = municipalityRepository;
		this.suburbRepository = suburbRepository;
		this.provinceRepository = provinceRepository;
		this.syncRunRepository = syncRunRepository;
	}

	public async ValueTask CreateSyncRunAsync(SyncRun syncRun, CancellationToken ct = default)
	{
		await syncRunRepository.AddAsync(syncRun, ct);
		await syncRunRepository.UnitOfWork.SaveEntitiesAsync(ct);
	}

	public async ValueTask<List<Province>> GetProvincesAsync(CancellationToken ct = default)
	{
		return await provinceRepository.GetAsync(ct);
	}

	public async ValueTask<int> UpsertMunicipalityAsync(IEnumerable<Municipality> municipalities, CancellationToken ct = default)
	{
		var result = await municipalityRepository.UpsertAsync(municipalities, ct);
		await municipalityRepository.UnitOfWork.SaveEntitiesAsync(ct);
		return result;
	}

	public async ValueTask<int> UpsertSuburbAsync(IEnumerable<Suburb> suburbs, CancellationToken ct = default)
	{
		var result = await suburbRepository.UpsertAsync(suburbs, ct);
		await suburbRepository.UnitOfWork.SaveEntitiesAsync(ct);
		return result;
	}
}
