namespace HEAppE.HpcConnectionFramework.Configuration
{
    /// <summary>
    /// Cluster scripts configuration
    /// </summary>
    public sealed class ScriptsConfiguration
    {
        #region Properties
        /// <summary>
        /// Cluster HEAppE Scripts GIT repository URI
        /// </summary>
        public string ClusterScriptsRepository { get; set; }

        /// <summary>
        /// .key_scripts HEAppE Scripts repository path
        /// </summary>
        public string KeyScriptsDirectory { get; set; }

        /// <summary>
        /// Command scripts path configuration
        /// </summary>
        public CommandScriptPathConfiguration CommandScriptsPathSettings { get; } = new CommandScriptPathConfiguration();

        /// <summary>
        /// Linux local command scripts path configuration
        /// </summary>
        public LinuxLocalCommandScriptPathConfiguration LinuxLocalCommandScriptPathSettings { get; } = new LinuxLocalCommandScriptPathConfiguration();
        #endregion
    }
}