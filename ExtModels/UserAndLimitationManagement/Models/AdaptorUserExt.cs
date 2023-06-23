using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models
{
    [DataContract(Name = "AdaptorUserExt")]
    public class AdaptorUserExt
    {
        [DataMember(Name = "Id")]
        public long? Id { get; set; }

        [DataMember(Name = "Username")]
        public string Username { get; set; }

        public override string ToString()
        {
            return $"AdaptorUserExt(id={Id}; username={Username})";
        }
    }
}
