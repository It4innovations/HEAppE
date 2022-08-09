using HEAppE.DomainObjects.Management;
using HEAppE.ExtModels.Management.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.ExtModels.Management.Converts
{
    public static class ManagementConverts
    {
        #region Public Methods
        public static DeploymentTypeExt ConvertIntToExt(this DeploymentType type)
        {
            _ = Enum.TryParse(type.ToString(), out DeploymentTypeExt convert);
            return convert;
        }

        public static ResourceAllocationTypeExt ConvertIntToExt(this ResourceAllocationType type)
        {
            _ = Enum.TryParse(type.ToString(), out ResourceAllocationTypeExt convert);
            return convert;
        }
        #endregion
    }
}
