using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.IRepository.Management;
using HEAppE.DomainObjects.Management;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.Management;

internal class SystemRoleAssignmentRepository : GenericRepository<SystemRoleAssignment>, ISystemRoleAssignmentRepository
{
    public SystemRoleAssignmentRepository(MiddlewareContext context) : base(context)
    {
    }

    public SystemRoleAssignment GetByUsernameAndRole(string username, AdaptorUserRoleType role)
    {
        return _dbSet.FirstOrDefault(x => x.Username == username && x.Role == role);
    }

    public async Task<SystemRoleAssignment> GetByUsernameAndRoleAsync(string username, AdaptorUserRoleType role)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.Username == username && x.Role == role);
    }

    public List<SystemRoleAssignment> GetAllByUsername(string username)
    {
        return _dbSet.Where(x => x.Username == username).ToList();
    }
}
