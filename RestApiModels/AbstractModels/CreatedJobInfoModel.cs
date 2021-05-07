using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.AbstractModels
{
    public abstract class CreatedJobInfoModel : SessionCodeModel
    {
        [DataMember(Name = "CreatedJobInfoId")]
        public long CreatedJobInfoId { get; set; }
    }
}