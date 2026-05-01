using GridMonitor.Domain.Entities;

namespace GridMonitor.Domain.Repositories;

public interface IMunicipalityRepository : IRepository
{
	ValueTask<Municipality> GetById(int id, CancellationToken ct = default);
	ValueTask<Municipality> GetByEskomIdAsync(int eskomId, CancellationToken ct = default);
	ValueTask<IList<Municipality>> GetListAsync(CancellationToken ct = default);
	ValueTask<IList<Municipality>> GetByProvinceAsync(int provinceId, CancellationToken ct = default);
	ValueTask<IList<Municipality>> GetBySearchPhrase(string searchPhrase, int limit, CancellationToken ct = default);
	ValueTask<int> UpsertAsync(IEnumerable<Municipality> municipalities, CancellationToken ct = default);
}
