using GridMonitor.Domain.Entities;

namespace GridMonitor.Domain.Repositories;

public interface ISuburbRepository : IRepository
{
	ValueTask<Suburb> GetByIdAsync(int id, CancellationToken ct = default);
	ValueTask<Suburb> GetByEskomIdAsync(int eskomId, CancellationToken ct = default);
	ValueTask<List<Suburb>> GetByMunicipalityAsync(int municipalityId, CancellationToken ct = default);
	ValueTask<List<Suburb>> SearchAsync(string searchPhrase, int limit, CancellationToken ct = default);
	ValueTask<int> UpsertAsync(IEnumerable<Suburb> suburbs, CancellationToken ct = default);
}
