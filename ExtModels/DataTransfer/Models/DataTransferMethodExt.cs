using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.DataTransfer.Models;

/// <summary>
/// Data transfer method ext
/// </summary>
[DataContract(Name = "DataTransferMethodExt")]
[Description("Data transfer method ext")]
public class DataTransferMethodExt
{
    #region Override Methods

    public override string ToString()
    {
        return
            $"DataTransferMethodExt(SubmittedTaskId: {SubmittedTaskId}, NodeIPAddress: {NodeIPAddress}, NodePort: {NodePort})";
    }

    #endregion

    #region Properties
    /// <summary>
    /// Submitted task id
    /// </summary>
    [DataMember(Name = "SubmittedTaskId")]
    [Description("Submitted task id")]
    public long SubmittedTaskId { get; set; }

    /// <summary>
    /// Port
    /// </summary>
    [DataMember(Name = "Port")]
    [Description("Port")]
    public int? Port { get; set; }

    /// <summary>
    /// Node ip address
    /// </summary>
    [DataMember(Name = "NodeIPAddress")]
    [StringLength(45)]
    [Description("Node ip address")]
    public string NodeIPAddress { get; set; }

    /// <summary>
    /// Node port
    /// </summary>
    [DataMember(Name = "NodePort")]
    [Description("Node port")]
    public int? NodePort { get; set; }

    #endregion
}