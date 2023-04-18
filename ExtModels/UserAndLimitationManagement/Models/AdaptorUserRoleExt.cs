using HEAppE.OpenStackAPI.DTO.JsonTypes.Authentication;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models
{
    [DataContract(Name = "AdaptorUserRoleExt")]
    public class AdaptorUserRoleExt
    {
        [Required]
        [DataMember(Name = "Name")]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [DataMember(Name = "Description")]
        [StringLength(200)]
        public string Description { get; set; }
        public override string ToString()
        {
            return $"AdaptorUserRoleExt(name={Name}; description={Description};)";
        }
    }
}
