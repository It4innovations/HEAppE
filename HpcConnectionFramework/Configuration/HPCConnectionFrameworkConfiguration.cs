using System.Collections.Generic;
using System.Dynamic;

namespace HEAppE.HpcConnectionFramework.Configuration
{
    /// <summary>
    /// HPC connection framework configuration
    /// </summary>
    public sealed class HPCConnectionFrameworkConfiguration
    {
        #region Properties
        /// <summary>
        /// Generic command key parameter
        /// </summary>
        public static string GenericCommandKeyParameter { get; set; }

        /// <summary>
        /// Database job array delimiter
        /// </summary>
        public static string JobArrayDbDelimiter { get; set; }
        
        /// <summary>
        /// Cluster HEAppE Scripts GIT repository URI
        /// </summary>
        public static string ClusterScriptsRepository { get; set; }
        
        /// <summary>
        /// .key_scripts HEAppE Scripts repository path
        /// </summary>
        public static string KeyScriptsDirectory { get; set; }

        /// <summary>
        /// Tunnel configuration
        /// </summary>
        public static TunnelConfiguration TunnelSettings { get; } = new TunnelConfiguration();

        /// <summary>
        /// Command scripts path configuration
        /// </summary>
        public static CommandScriptPathConfiguration CommandScriptsPathSettings { get; } = new CommandScriptPathConfiguration();

        /// <summary>
        /// Linux local command scripts path configuration
        /// </summary>
        public static LinuxLocalCommandScriptPathConfiguration LinuxLocalCommandScriptPathSettings { get; } = new LinuxLocalCommandScriptPathConfiguration();

        /// <summary>
        /// Clusters connection Pool configuration
        /// </summary>
        public static ClusterConnectionPoolConfiguration ClustersConnectionPoolSettings { get; } = new ClusterConnectionPoolConfiguration();
        #endregion
    }
}
