using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models
{
    [DataContract(Name = "AuthenticationCredentialsExt")]
    public class AuthenticationCredentialsExt
    {
        [DataMember(Name = "UserName"), StringLength(50)]
        public string Username { get; set; }

        public override string ToString()
        {
            return $"""AuthenticationCredentialsExt(Username="{Username}")""";
        }
    }
}
