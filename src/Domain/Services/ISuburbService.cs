using GridMonitor.Domain.Entities;

namespace GridMonitor.Domain.Services;

public interface ISuburbService
{
	ValueTask<IList<Suburb>> GetBySearchPhraseAsync(string searchPhrase, int limit, CancellationToken ct = default);
	ValueTask<IList<Municipality>> GetMunicipalitiesAsync(int provinceId, CancellationToken ct = default);
}