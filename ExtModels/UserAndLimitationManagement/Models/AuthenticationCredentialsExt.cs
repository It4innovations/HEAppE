using System;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models
{
    [DataContract(Name = "AuthenticationCredentialsExt")]
    public class AuthenticationCredentialsExt
    {
        [DataMember(Name = "UserName")]
        public string Username { get; set; }

        public override string ToString()
        {
            return $"AuthenticationCredentialsExt(username={Username})";
        }
    }
}
