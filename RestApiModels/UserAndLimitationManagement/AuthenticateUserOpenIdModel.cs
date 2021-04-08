using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.RestApiModels.UserAndLimitationManagement
{
    [DataContract(Name = "AuthenticateUserOpenIdModel")]
    public class AuthenticateUserOpenIdModel
    {
        [DataMember(Name = "Credentials")]
        public OpenIdCredentialsExt Credentials { get; set; }
    }
}
