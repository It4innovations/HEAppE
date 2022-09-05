using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.RestApiModels.UserAndLimitationManagement
{
    [DataContract(Name = "AuthenticateUserDigitalSignatureModel")]
    public class AuthenticateUserDigitalSignatureModel
    {
        [DataMember(Name = "Credentials")]
        public DigitalSignatureCredentialsExt Credentials { get; set; }
        public override string ToString()
        {
            return $"AuthenticateUserDigitalSignatureModel({base.ToString()}; Credentials: {Credentials})";
        }
    }
}
