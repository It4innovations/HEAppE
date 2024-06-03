using HEAppE.DomainObjects.ClusterInformation;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.ClusterInformation
{
    public interface IClusterNodeTypeRepository : IRepository<ClusterNodeType>
    {
        IEnumerable<ClusterNodeType> GetAllWithPossibleCommands();
        IEnumerable<ClusterNodeType> GetAllByFileTransferMethod(long fileTransferMethodId);
    }
}