using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DataAccessTier.IRepository.ClusterInformation;

public interface IClusterNodeTypeRepository : IRepository<ClusterNodeType>
{
    IEnumerable<ClusterNodeType> GetAllWithPossibleCommands();
    Task<IEnumerable<ClusterNodeType>> GetAllWithPossibleCommandsAsync();
    IEnumerable<ClusterNodeType> GetAllByFileTransferMethod(long fileTransferMethodId);
    Task<IEnumerable<ClusterNodeType>> GetAllByFileTransferMethodAsync(long fileTransferMethodId);
    ClusterNodeType GetByIdWithClusterAndProjects(long id);
    Task<ClusterNodeType> GetByIdWithClusterAndProjectsAsync(long id);
}