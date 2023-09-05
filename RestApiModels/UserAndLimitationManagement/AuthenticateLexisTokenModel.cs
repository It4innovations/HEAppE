using System.Runtime.Serialization;

using HEAppE.ExtModels.UserAndLimitationManagement.Models;

namespace HEAppE.RestApiModels.UserAndLimitationManagement
{
  [DataContract(Name = "AuthenticateLexisTokenModel")]
  public class AuthenticateLexisTokenModel
  {
    [DataMember(Name = "Credentials")]
    public OpenIdCredentialsExt Credentials { get; set; }

    public override string ToString()
    {
      return $"AuthenticateUserOpenIdModel({base.ToString()}; Credentials: {Credentials})";
    }
  }
}
