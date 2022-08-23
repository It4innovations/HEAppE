using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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