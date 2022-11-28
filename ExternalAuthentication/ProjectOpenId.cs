using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.ExternalAuthentication
{
    public class ProjectOpenId
    {
        /// <summary>
        /// UUID
        /// </summary>
        public string UUID { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Group name in HEAppE DB
        /// </summary>
        public string HEAppEGroupName { get; set; }

        /// <summary>
        /// HEAppE group
        /// </summary>
        public IEnumerable<string> Roles { get; set; }
    }
}
