using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.DataTransfer;

/// <summary>
/// Model for retrieving data transfer method
/// </summary>
[DataContract(Name = "GetDataTransferMethodModel")]
[Description("Model for retrieving data transfer method")]
public class GetDataTransferMethodModel : SubmittedJobInfoModel
{
    /// <summary>
    /// Ip address
    /// </summary>
    [DataMember(Name = "IpAddress")]
    [StringLength(50)]
    [Description("Ip address")]
    public string IpAddress { get; set; }

    /// <summary>
    /// Port
    /// </summary>
    [DataMember(Name = "Port")]
    [Description("Port")]
    public int Port { get; set; }

    public override string ToString()
    {
        return $"GetDataTransferMethodModel({base.ToString()}; IpAddress: {IpAddress}; Port: {Port})";
    }
}