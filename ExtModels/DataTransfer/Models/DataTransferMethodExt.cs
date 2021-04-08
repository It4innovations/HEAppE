using System.Runtime.Serialization;

namespace HEAppE.ExtModels.DataTransfer.Models
{
    [DataContract(Name = "DataTransferMethodExt")]
    public class DataTransferMethodExt
    {
        #region Properties
        [DataMember(Name = "SubmittedJobId")]
        public long SubmittedJobId { get; set; }

        [DataMember(Name = "IpAddress")]
        public string IpAddress { get; set; }

        [DataMember(Name = "Port")]
        public int Port { get; set; }
        #endregion
        #region Override Methods
        public override string ToString()
        {
            return $"DataTransferMethodExt(SubmittedJobId: {SubmittedJobId}, IpAddress: {IpAddress}, Port: {Port})";
        }
        #endregion
    }
}
