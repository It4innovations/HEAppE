using System.Linq;
using HEAppE.DataAccessTier.IRepository.OpenStack;
using HEAppE.DomainObjects.OpenStack;

namespace HEAppE.DataAccessTier.Repository.OpenStack
{
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
            return GetAll().SingleOrDefault(instance => instance.Name == instanceName);
        }
        #endregion
    }
}