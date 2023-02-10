using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models
{
    [DataContract(Name = "OpenIdOpenStackCredentialsExt")]
    public class OpenIdOpenStackCredentialsExt : OpenIdCredentialsExt
    {
        /// <summary>
        /// ProjectId.
        /// </summary>
        [Required]
        [DataMember(Name = nameof(ProjectId))]
        public long ProjectId { get; set; }

        public override string ToString()
        {
            return $"OpenIdOpenStackCredentialsExt({base.ToString()}; ProjectId='{ProjectId}')";
        }
    }
}
