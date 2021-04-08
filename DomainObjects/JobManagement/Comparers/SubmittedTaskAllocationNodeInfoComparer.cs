using HEAppE.DomainObjects.JobManagement.JobInformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DomainObjects.JobManagement.Comparers
{
    public class SubmittedTaskAllocationNodeInfoComparer : IEqualityComparer<SubmittedTaskAllocationNodeInfo>
    {
        public bool Equals(SubmittedTaskAllocationNodeInfo x, SubmittedTaskAllocationNodeInfo y)
        {
            return x.AllocationNodeId == y.AllocationNodeId && x.SubmittedTaskInfoId == y.SubmittedTaskInfoId;
        }

        public int GetHashCode(SubmittedTaskAllocationNodeInfo obj)
        {
            return HashCode.Combine(obj.AllocationNodeId.GetHashCode(), obj.SubmittedTaskInfoId.GetHashCode());
        }
    }
}
