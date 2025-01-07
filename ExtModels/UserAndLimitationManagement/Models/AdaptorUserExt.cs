using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

[DataContract(Name = "AdaptorUserExt")]
public class AdaptorUserExt
{
    [DataMember(Name = "Id")] public long? Id { get; set; }

    [DataMember(Name = "Username")] public string Username { get; set; }

    [DataMember(Name = "PublicKey")] public string PublicKey { get; set; }

    [DataMember(Name = "Email")] public string Email { get; set; }

    [DataMember(Name = "UserType")] public AdaptorUserTypeExt UserType { get; set; }

    [DataMember(Name = "AdaptorUserGroups")]
    public AdaptorUserGroupExt[] AdaptorUserGroups { get; set; }

    public override string ToString()
    {
        return
            $"AdaptorUserExt(id={Id}; username={Username}; publicKey={PublicKey}; email={Email}; userType={UserType}; adaptorUserGroups={AdaptorUserGroups})";
    }
}