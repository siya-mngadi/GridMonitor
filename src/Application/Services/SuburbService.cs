using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Domain.Services;

namespace GridMonitor.Application.Services;

public class SuburbService : ISuburbService
{
	private readonly ISuburbRepository suburbRepository;
	private readonly IMunicipalityRepository municipalityRepository;

	public SuburbService(ISuburbRepository suburbRepository, IMunicipalityRepository municipalityRepository)
	{
		this.suburbRepository = suburbRepository;
		this.municipalityRepository = municipalityRepository;
	}

	public async ValueTask<IList<Suburb>> GetBySearchPhraseAsync(string searchPhrase, int limit, CancellationToken ct = default)
	{
		return await suburbRepository.SearchAsync(searchPhrase, limit, ct);
	}

	public async ValueTask<IList<Municipality>> GetMunicipalitiesAsync(int provinceId, CancellationToken ct = default)
	{
		return await municipalityRepository.GetByProvinceAsync(provinceId, ct);
	}
}
