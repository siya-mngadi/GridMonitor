using GridMonitor.Domain.Entities;

namespace GridMonitor.Domain.Services;

public interface ISuburbService
{
	ValueTask<List<Suburb>> GetBySearchPhraseAsync(string searchPhrase, int limit, CancellationToken ct = default);
	ValueTask<List<Municipality>> GetMunicipalitiesAsync(int provinceId, CancellationToken ct = default);
}