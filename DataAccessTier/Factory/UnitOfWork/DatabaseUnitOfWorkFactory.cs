using HEAppE.DataAccessTier.UnitOfWork;

namespace HEAppE.DataAccessTier.Factory.UnitOfWork;

internal class DatabaseUnitOfWorkFactory : UnitOfWorkFactory
{
    public override IUnitOfWork CreateUnitOfWork()
    {
        return new DatabaseUnitOfWork();
    }
}