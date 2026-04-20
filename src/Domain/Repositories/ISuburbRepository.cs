using GridMonitor.Domain.Entities;

namespace GridMonitor.Domain.Repositories;

public interface ISuburbRepository : IRepository
{
	ValueTask<IList<Suburb>> GetAsync(CancellationToken ct = default);
	ValueTask<Suburb> GetByIdAsync(int id, CancellationToken ct = default);
	ValueTask<Suburb> GetByEskomIdAsync(int eskomId, CancellationToken ct = default);
	ValueTask<IList<Suburb>> GetByMunicipalityAsync(int municipalityId, CancellationToken ct = default);
	ValueTask<IList<Suburb>> SearchAsync(string searchPhrase, int limit, CancellationToken ct = default);
	ValueTask<int> UpsertAsync(IEnumerable<Suburb> suburbs, CancellationToken ct = default);
}
