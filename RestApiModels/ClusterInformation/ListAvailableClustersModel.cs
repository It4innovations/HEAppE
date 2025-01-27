using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.ClusterInformation;

/// <summary>
/// Model for retrieving current cluster node usage
/// </summary>
[DataContract(Name = "ListAvailableClustersModel")]
[Description("Model for retrieving clusters")]
public class ListAvailableClustersModel : SessionCodeModel
{
    public override string ToString()
    {
        return $"ListAvailableClustersModel({base.ToString()})";
    }
}