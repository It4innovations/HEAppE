using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models
{
    [DataContract(Name = "NodeUsedCoresAndLimitationExt")]
    public class NodeUsedCoresAndLimitationExt
    {
        public int CoresUsed { get; set; } = 0;
        public override string ToString()
        {
            return $"NodeUsedCoresAndLimitationExt: CoresUsed={CoresUsed}";
        }
    }
}