using System.Threading.Tasks;
using HEAppE.DomainObjects.OpenStack;

namespace HEAppE.DataAccessTier.IRepository.OpenStack;

public interface IOpenStackInstanceRepository : IRepository<OpenStackInstance>
{
    OpenStackInstance GetByName(string instanceName);
    Task<OpenStackInstance> GetByNameAsync(string instanceName);
}