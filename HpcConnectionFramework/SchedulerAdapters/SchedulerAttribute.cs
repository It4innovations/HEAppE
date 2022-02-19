using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class SchedulerAttribute : Attribute
    {
        #region Properties
        internal IEnumerable<string> Names { get; set; }
        #endregion
        #region Contructors
        public SchedulerAttribute(params string[] names)
        {
            Names = names;
        }
        #endregion
    }
}
