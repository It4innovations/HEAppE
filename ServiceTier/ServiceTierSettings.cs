using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.ServiceTier
{
    public static class ServiceTierSettings
    {

        /// <summary>
        /// ProjectId set if HEAppE runs at single project instance mode
        /// </summary>
        public static long? SingleProjectId { get; set; } = null;
    }
}
