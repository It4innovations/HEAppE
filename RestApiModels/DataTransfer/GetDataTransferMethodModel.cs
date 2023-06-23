using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.DataTransfer
{
    [DataContract(Name = "GetDataTransferMethodModel")]
    public class GetDataTransferMethodModel : SubmittedJobInfoModel
    {
        [DataMember(Name = "IpAddress"), StringLength(50)]
        public string IpAddress { get; set; }

        [DataMember(Name = "Port")]
        public int Port { get; set; }
        public override string ToString()
        {
            return $"GetDataTransferMethodModel({base.ToString()}; IpAddress: {IpAddress}; Port: {Port})";
        }

    }
}
