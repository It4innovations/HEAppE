using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public static string GenericCommandKeyParameter { get; set; } = "#HEAPPE_PARAM";

        /// <summary>
        /// Database Job Array delimiter
        /// </summary>
        public static string JobArrayDbDelimiter { get; set; } = "<JOB_ARRAY_ITERATION>";
        #endregion
    }
}
