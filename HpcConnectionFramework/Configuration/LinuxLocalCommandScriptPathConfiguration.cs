namespace HEAppE.HpcConnectionFramework.Configuration
{
    /// <summary>
    /// Cluster scripts used in LINUX local
    /// </summary>
    public sealed class LinuxLocalCommandScriptPathConfiguration
    {
        #region Instances
        private string _scriptsBasePath = "~/.local_hpc_scripts";
        #endregion
        #region Properties
        /// <summary>
        /// Script Base Path
        /// </summary>
        public string ScriptsBasePath
        {
            get
            {
                return _scriptsBasePath;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _scriptsBasePath = value.Replace("\\","/").TrimEnd('/');
                }
            }
        }

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