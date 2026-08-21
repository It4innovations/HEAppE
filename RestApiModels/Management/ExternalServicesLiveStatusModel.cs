using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Model for retrieving external services live status
/// </summary>
[DataContract(Name = "ExternalServicesLiveStatusModel")]
[Description("External services live status model")]
public class ExternalServicesLiveStatusModel : SessionCodeModel
{
    public override string ToString()
    {
        return $"ExternalServicesLiveStatusModel({base.ToString()})";
    }
}
