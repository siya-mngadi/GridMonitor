using GridMonitor.Domain.Entities;

namespace GridMonitor.Domain.Repositories;

public interface IMunicipalityRepository : IRepository
{
	ValueTask<Municipality> GetById(int id, CancellationToken ct = default);
	ValueTask<Municipality> GetByEskomId(int eskomId, CancellationToken ct = default);
	ValueTask<List<Municipality>> GetByProvinceAsync(int provinceId, CancellationToken ct = default);
	ValueTask<List<Municipality>> GetBySearchPhrase(string searchPhrase, int limit, CancellationToken ct = default);
	ValueTask<int> UpsertAsync(IEnumerable<Municipality> municipalities, CancellationToken ct = default);
}
