using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.UserAndLimitationManagement
{
    [DataContract(Name = "AuthenticateUserOpenIdOpenStackModel")]
    public class AuthenticateUserOpenIdOpenStackModel : AuthenticateUserOpenIdModel
    {
        [DataMember(Name = "ProjectId")]
        public long ProjectId { get; set; }

        public override string ToString()
        {
            return $"AuthenticateUserOpenIdOpenStackModel({base.ToString()}; ProjectId: {ProjectId})";
        }
    }
}
