using HEAppE.DataAccessTier.UnitOfWork;

namespace HEAppE.BusinessLogicTier.Logic.AdminUserManagement;

internal class AdminUserManagementLogic : IAdminUserManagementLogic
{
    private readonly IUnitOfWork unitOfWork;

    internal AdminUserManagementLogic(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }
}