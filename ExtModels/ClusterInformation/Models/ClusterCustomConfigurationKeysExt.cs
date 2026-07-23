using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

/// <summary>
///     Cluster custom configuration keys ext
/// </summary>
[DataContract(Name = "ClusterCustomConfigurationKeysExt")]
[Description("Supported keys for Cluster.CustomConfiguration metadata dictionary")]
public enum ClusterCustomConfigurationKeysExt
{
    SshCommandPrefix = 1,
    SyncScriptsViaSftp = 2,
    ClusterScriptsRepository = 3,
    ClusterScriptsRepositoryBranch = 4,
    KeyScriptsDirectoryInRepository = 5,
    InstanceIdentifierPath = 6,
    SubExecutionsPath = 7,
    JobLogArchiveSubPath = 8,
    SubScriptsPath = 9,
    ScriptsBasePath = 10,
    EventualConsistencyRetryCount = 11,
    EventualConsistencyRetryDelayMs = 12,
    IdpUrl = 13,
    FirecrestUrl = 14,
    ClientId = 15,
    ClientSecret = 16,
    ClusterName = 17
}
