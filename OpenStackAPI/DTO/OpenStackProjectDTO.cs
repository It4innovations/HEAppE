using System.Collections.Generic;

namespace HEAppE.OpenStackAPI.DTO
{
    public class OpenStackProjectDTO
    {
        #region Properties
        public string Id { get; set; }

        public string Name { get; set; }

        public IEnumerable<OpenStackProjectDomainDTO> ProjectDomains { get; set; } =  new List<OpenStackProjectDomainDTO>();
        #endregion
    }
}
