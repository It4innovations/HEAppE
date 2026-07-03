using System.Linq;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.IRepository.OpenStack;
using HEAppE.DomainObjects.OpenStack;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.OpenStack;

internal class OpenStackProjectRepository : GenericRepository<OpenStackProject>, IOpenStackProjectRepository
{
    #region Constructors

    internal OpenStackProjectRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Methods

    public OpenStackProject GetOpenStackProjectByProjectId(long projectId)
    {
        return _dbSet
            .Include(p => p.AdaptorUserGroup)
            .Include(p => p.OpenStackProjectDomain)
            .Include(p => p.OpenStackDomain)
                .ThenInclude(d => d.OpenStackInstance)
            .Include(p => p.OpenStackAuthenticationCredentialProjects)
                .ThenInclude(cp => cp.OpenStackAuthenticationCredential)
            .FirstOrDefault(f => f.AdaptorUserGroup.ProjectId == projectId);
    }

    public async Task<OpenStackProject> GetOpenStackProjectByProjectIdAsync(long projectId)
    {
        return await _dbSet
            .Include(p => p.AdaptorUserGroup)
            .Include(p => p.OpenStackProjectDomain)
            .Include(p => p.OpenStackDomain)
                .ThenInclude(d => d.OpenStackInstance)
            .Include(p => p.OpenStackAuthenticationCredentialProjects)
                .ThenInclude(cp => cp.OpenStackAuthenticationCredential)
            .FirstOrDefaultAsync(f => f.AdaptorUserGroup.ProjectId == projectId);
    }

    #endregion
}