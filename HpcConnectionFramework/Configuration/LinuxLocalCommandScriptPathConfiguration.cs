namespace HEAppE.HpcConnectionFramework.Configuration
{
    /// <summary>
    /// Cluster scripts used in LINUX local
    /// </summary>
    public sealed class LinuxLocalCommandScriptPathConfiguration
    {
        #region Properties
        /// <summary>
        /// Path to Prepare LocalHPC job directory
        /// </summary>
        public string PrepareJobDirCmdScriptName { get; set; }

        /// <summary>
        /// Run local job execution simulation
        /// </summary>
        public string RunLocalCmdScriptName { get; set; }

        /// <summary>
        /// Path to execute job info get cmd
        /// </summary>
        public string GetJobInfoCmdScriptName { get; set; }

        /// <summary>
        /// Path to execute count jobs
        /// </summary>
        public string CountJobsCmdScriptName { get; set; }

        /// <summary>
        /// Path to execute cancel simulated job
        /// </summary>
        public string CancelJobCmdScriptName { get; set; }
        #endregion
    }
}