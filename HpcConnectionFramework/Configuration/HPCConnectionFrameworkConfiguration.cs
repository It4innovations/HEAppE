using System.Collections.Generic;

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
        public static Dictionary<string, ClusterConnectionPoolConfiguration> ClustersConnectionPoolSettings { get; } = new Dictionary<string, ClusterConnectionPoolConfiguration>();
        #endregion
    }
}
