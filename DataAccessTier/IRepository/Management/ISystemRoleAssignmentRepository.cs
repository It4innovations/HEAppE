using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.Management;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;

namespace HEAppE.DataAccessTier.IRepository.Management;

public interface ISystemRoleAssignmentRepository : IRepository<SystemRoleAssignment>
{
    SystemRoleAssignment GetByUsernameAndRole(string username, AdaptorUserRoleType role);
    Task<SystemRoleAssignment> GetByUsernameAndRoleAsync(string username, AdaptorUserRoleType role);
    List<SystemRoleAssignment> GetAllByUsername(string username);
}
