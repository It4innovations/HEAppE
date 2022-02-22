namespace HEAppE.HpcConnectionFramework.Configuration
{
    /// <summary>
    /// Clusters scripts used in schedulers
    /// </summary>
    internal sealed class CommandScriptPathConfiguration
    {
        #region Properties
        /// <summary>
        /// Path to copy data from temp script
        /// </summary>
        internal string CopyDataFromTempCmdPath { get; } = "~/.key_scripts/copy_data_from_temp.sh"; 

        /// <summary>
        /// Path to copy data to temp script
        /// </summary>
        internal string CopyDataToTempCmdPath { get; } = "~/.key_scripts/copy_data_to_temp.sh";

        /// <summary>
        /// Path to adding file transfer key script
        /// </summary>
        internal string AddFiletransferKeyCmdPath { get; } = "~/.key_scripts/add_key.sh";

        /// <summary>
        /// Path to remove file transfer key script
        /// </summary>
        internal string RemoveFiletransferKeyCmdPath { get; } = "~/.key_scripts/remove_key.sh";

        /// <summary>
        /// Path to create job directory script
        /// </summary>
        internal string CreateJobDirectoryCmdPath { get; } = "~/.key_scripts/create_job_directory.sh";

        /// <summary>
        /// Path to execute command from Base64
        /// </summary>
        internal string ExecutieCmdPath { get; } = "~/.key_scripts/run_command.sh.sh";
        #endregion
    }
}
