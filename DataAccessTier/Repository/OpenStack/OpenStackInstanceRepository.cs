using System.Linq;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.IRepository.OpenStack;
using HEAppE.DomainObjects.OpenStack;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.OpenStack;

internal class OpenStackInstanceRepository : GenericRepository<OpenStackInstance>, IOpenStackInstanceRepository
{
    #region Constructors

    internal OpenStackInstanceRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Methods

    public OpenStackInstance GetByName(string instanceName)
    {
        return _dbSet.SingleOrDefault(instance => instance.Name == instanceName);
    }

    public async Task<OpenStackInstance> GetByNameAsync(string instanceName)
    {
        return await _dbSet.SingleOrDefaultAsync(instance => instance.Name == instanceName);
    }

    #endregion
}