using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.DataTransfer.Models
{
    [DataContract(Name = "DataTransferMethodExt")]
    public class DataTransferMethodExt
    {
        #region Properties
        [DataMember(Name = "SubmittedTaskId")]
        public long SubmittedTaskId { get; set; }

        [DataMember(Name = "Port")]
        public int? Port { get; set; }

        [DataMember(Name = "NodeIPAddress"), StringLength(45)]
        public string NodeIPAddress { get; set; }

        [DataMember(Name = "NodePort")]
        public int? NodePort { get; set; }
        #endregion
        #region Override Methods
        public override string ToString()
        {
            return $"DataTransferMethodExt(SubmittedTaskId: {SubmittedTaskId}, NodeIPAddress: {NodeIPAddress}, NodePort: {NodePort})";
        }
        #endregion
    }
}
