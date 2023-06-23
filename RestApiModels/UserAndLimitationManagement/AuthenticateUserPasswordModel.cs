using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.UserAndLimitationManagement
{
    [DataContract(Name = "AuthenticateUserPasswordModel")]
    public class AuthenticateUserPasswordModel
    {
        [DataMember(Name = "Credentials")]
        public PasswordCredentialsExt Credentials { get; set; }
        public override string ToString()
        {
            return $"AuthenticateUserPasswordModel({base.ToString()}; Credentials: {Credentials})";
        }
    }
}