using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

[DataContract(Name = "OpenIdCredentialsExt")]
public class OpenIdCredentialsExt : AuthenticationCredentialsExt
{
  /// <summary>
  ///     OpenId access token.
  /// </summary>
  [Required]
    [DataMember(Name = nameof(OpenIdAccessToken))]
    public string OpenIdAccessToken { get; set; }

    public override string ToString()
    {
        return $"OpenIdCredentialsExt({base.ToString()}; access_token='{OpenIdAccessToken}')";
    }
}