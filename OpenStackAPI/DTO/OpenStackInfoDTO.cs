using System.Collections.Generic;

namespace HEAppE.OpenStackAPI.DTO
{
    public class OpenStackInfoDTO
    {
        #region Properties
        public IEnumerable<OpenStackProjectDTO> Projects { get; set; } = new List<OpenStackProjectDTO>();

        public OpenStackServiceAccDTO ServiceAcc { get; set; }
        #endregion
    }
}
