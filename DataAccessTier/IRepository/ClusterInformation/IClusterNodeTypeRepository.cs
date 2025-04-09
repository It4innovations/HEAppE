using System.Collections.Generic;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DataAccessTier.IRepository.ClusterInformation;

public interface IClusterNodeTypeRepository : IRepository<ClusterNodeType>
{
    IEnumerable<ClusterNodeType> GetAllWithPossibleCommands();
    IEnumerable<ClusterNodeType> GetAllByFileTransferMethod(long fileTransferMethodId);
}