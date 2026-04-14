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

	public IAsyncEnumerable<Suburb> GetBySearchPhrase(string searchPhrase)
	{
		return suburbRepository.GetBySearchPhrase(searchPhrase);
	}

	public IAsyncEnumerable<Municipality> GetMunicipalities(int provinceId)
	{
		return municipalityRepository.GetByProvince(provinceId);
	}
}
