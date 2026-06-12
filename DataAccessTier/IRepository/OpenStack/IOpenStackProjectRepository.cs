using System.Threading.Tasks;
using HEAppE.DomainObjects.OpenStack;

namespace HEAppE.DataAccessTier.IRepository.OpenStack;

public interface IOpenStackProjectRepository : IRepository<OpenStackProject>
{
    OpenStackProject GetOpenStackProjectByProjectId(long projectId);
    Task<OpenStackProject> GetOpenStackProjectByProjectIdAsync(long projectId);
}