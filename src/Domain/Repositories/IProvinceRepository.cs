using GridMonitor.Domain.Entities;

namespace GridMonitor.Domain.Repositories;

public interface IProvinceRepository : IRepository
{
	ValueTask<List<Province>> GetAsync(CancellationToken ct = default);
	ValueTask<Province> GetByIdAsync(int id, CancellationToken ct = default);
	ValueTask<Province> GetByEskomIdAsync(int eskomId, CancellationToken ct = default);
	ValueTask<Province> GetByNameAsync(string name, CancellationToken ct = default);
	ValueTask<Province> UpsertAsync(Province province, CancellationToken ct = default);
}
