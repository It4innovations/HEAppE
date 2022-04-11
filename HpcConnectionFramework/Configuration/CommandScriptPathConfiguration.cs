namespace HEAppE.HpcConnectionFramework.Configuration
{
    /// <summary>
    /// Clusters scripts used in schedulers
    /// </summary>
    public sealed class CommandScriptPathConfiguration
    {
        #region Properties
        /// <summary>
        /// Path to adding file transfer key script
        /// </summary>
        public string AddFiletransferKeyCmdPath { get; set; } 

        /// <summary>
        /// Path to remove file transfer key script
        /// </summary>
        public string RemoveFiletransferKeyCmdPath { get; set; }

        /// <summary>
        /// Path to create job directory script
        /// </summary>
        public string CreateJobDirectoryCmdPath { get; set; }

        /// <summary>
        /// Path to execute command from Base64
        /// </summary>
        public string ExecutieCmdPath { get; set; }

        /// <summary>
        /// Path to copy data from temp script
        /// </summary>
        public string CopyDataFromTempCmdPath { get; set; }

        /// <summary>
        /// Path to copy data to temp script
        /// </summary>
        public string CopyDataToTempCmdPath { get; set; }
        #endregion
    }
}
