using System;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters
{
    /// <summary>
    /// Scheduler attributes mapping for parsing
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SchedulerAttribute : Attribute
    {
        #region Properties
        /// <summary>
        /// Parsing format
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Mapping names
        /// </summary>
        public IEnumerable<string> Names { get; set; }
        #endregion
        #region Contructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="names">Mapping names</param>
        public SchedulerAttribute(params string[] names)
        {
            Names = names;
            Format = default;
        }
        #endregion
    }
}
