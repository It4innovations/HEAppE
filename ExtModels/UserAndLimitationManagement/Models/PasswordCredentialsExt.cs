using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models
{
    [DataContract(Name = "PasswordCredentialsExt")]
    public class PasswordCredentialsExt : AuthenticationCredentialsExt
    {
        //public string username { get; set; }
        [DataMember(Name = "Password"), StringLength(50)]
        public string Password { get; set; }

        public override string ToString()
        {
            return $"PasswordCredentialsExt({base.ToString()}; password={Password})";
        }
    }
}
