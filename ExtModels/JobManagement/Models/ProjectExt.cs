using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.ExtModels.JobManagement.Models
{
    [DataContract(Name = "ProjectExt")]
    public class ProjectExt
    {
        [DataMember(Name = "Id")]
        public long Id { get; set; }
        [DataMember(Name = "AccountingString")]
        public string AccountingString { get; set; }
        [DataMember(Name = "StartDate")]
        public DateTime StartDate { get; set; }
        [DataMember(Name = "EndDate")]
        public DateTime EndDate { get; set; }

        #region Public methods
        public override string ToString()
        {
            return String.Format($"Project: Id={Id}, AccountingString={AccountingString}, StartDate={StartDate}, EndDate={EndDate}");
        }
        #endregion
    }
}
