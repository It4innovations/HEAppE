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
        public static CommandScriptPathConfiguration CommandScriptsPathConfiguration { get; } = new CommandScriptPathConfiguration();

        /// <summary>
        /// Linux local command scripts path configuration
        /// </summary>
        public static LinuxLocalCommandScriptPathConfiguration LinuxLocalCommandScriptPathConfiguration { get; } = new LinuxLocalCommandScriptPathConfiguration();
        #endregion
    }
}
