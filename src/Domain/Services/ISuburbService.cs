using GridMonitor.Domain.Entities;

namespace GridMonitor.Domain.Services;

public interface ISuburbService
{
	IAsyncEnumerable<Suburb> GetBySearchPhrase(string searchPhrase);
	IAsyncEnumerable<Municipality> GetMunicipalities(int provinceId);
}