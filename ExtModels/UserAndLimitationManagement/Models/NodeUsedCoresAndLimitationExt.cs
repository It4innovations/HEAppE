using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

/// <summary>
/// Used cores and limitation of node ext
/// </summary>
[DataContract(Name = "NodeUsedCoresAndLimitationExt")]
[Description("Used cores and limitation of node ext")]
public class NodeUsedCoresAndLimitationExt
{
    /// <summary>
    /// Number of cores used
    /// </summary>
    [Description("Number of cores used")]
    public int CoresUsed { get; set; } = 0;

    public override string ToString()
    {
        return $"NodeUsedCoresAndLimitationExt: CoresUsed={CoresUsed}";
    }
}