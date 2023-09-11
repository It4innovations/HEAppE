using System;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.FileTransfer.Models
{
    [DataContract(Name = "FileInformationExt")]
    public class FileInformationExt
    {
        [DataMember(Name = "FileName")]
        public string FileName { get; set; }

        [DataMember(Name = "LastModifiedDate")]
        public DateTime? LastModifiedDate { get; set; }

        public override string ToString()
        {
            return $"FileInformationExt({base.ToString()}; FileName={FileName}; LastModifiedDate={LastModifiedDate})";
        }
    }
}
