using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.OpenStackAPI.DTO
{
    public class OpenStackInfoDTO
    {
        #region Properties
        public string Domain { get; set; }

        public string Project { get; set; }

        public string OpenStackUrl { get; set; }

        public string ServiceAccUsername { get; set; }

        public string ServiceAccPassword { get; set; }
        #endregion
    }
}
