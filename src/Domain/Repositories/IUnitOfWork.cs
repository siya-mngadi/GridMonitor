namespace GridMonitor.Domain.Repositories;

public interface IUnitOfWork
{
	ValueTask<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}