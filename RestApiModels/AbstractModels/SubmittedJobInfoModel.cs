using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.AbstractModels
{
    public abstract class SubmittedJobInfoModel : SessionCodeModel
    {
        [DataMember(Name = "SubmittedJobInfoId")]
        public long SubmittedJobInfoId { get; set; }
        public override string ToString()
        {
            return $"SubmittedJobInfoModel({base.ToString()}; SubmittedJobInfoId: {SubmittedJobInfoId})";
        }
    }
}