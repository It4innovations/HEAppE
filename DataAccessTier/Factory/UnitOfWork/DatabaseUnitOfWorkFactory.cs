using HEAppE.DataAccessTier.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace HEAppE.DataAccessTier.Factory.UnitOfWork;

internal class DatabaseUnitOfWorkFactory : UnitOfWorkFactory
{
    public override IUnitOfWork CreateUnitOfWork()
    {
        return new DatabaseUnitOfWork();
    }

    public override IUnitOfWork CreateUnitOfWork(ILogger logger)
    {
        return new DatabaseUnitOfWork(logger);
    }
}