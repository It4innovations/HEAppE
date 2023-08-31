using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models
{
  [DataContract(Name = "LexisCredentialsExt")]
  public class LexisCredentialsExt : AuthenticationCredentialsExt
  {
    /// <summary>
    /// OpenId access token.
    /// </summary>
    [Required]
    [DataMember(Name = nameof(OpenIdAccessToken))]
    public string OpenIdAccessToken { get; set; }

    public override string ToString()
    {
      return $"LexisCredentialsExt({base.ToString()}; access_token='{OpenIdAccessToken}')";
    }
  }
}
