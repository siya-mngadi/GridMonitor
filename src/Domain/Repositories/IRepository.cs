namespace GridMonitor.Domain.Repositories;

public interface IRepository
{
	IUnitOfWork UnitOfWork { get; }
}