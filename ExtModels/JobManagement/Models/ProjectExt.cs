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
        [DataMember(Name = "AccountingString")]
        public string AccountingString;
        [DataMember(Name = "StartDate")]
        public DateTime StartDate;
        [DataMember(Name = "EndDate")]
        public DateTime EndDate;

        #region Public methods
        public override string ToString()
        {
            return String.Format($"Project: AccountingString={AccountingString}, StartDate={StartDate}, EndDate={EndDate}");
        }
        #endregion
    }
}
