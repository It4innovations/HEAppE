using System.ComponentModel;
using System.Runtime.Serialization;

namespace Services.Expirio.Models;

/// <summary>
/// Kerberos exchange response
/// </summary>
[DataContract(Name = "KerberosExchangeResponse")]
[Description("Kerberos Exchange response")]
public class KerberosExchangeResponse
{
    /// <summary>
    /// FileName
    /// </summary>
    [DataMember(Name = "FileName")]
    [Description("FileName")]
    public required string FileName { get; set; }

    /// <summary>
    /// Content
    /// </summary>
    [DataMember(Name = "Content")]
    [Description("Content")]
    public required string Content { get; set; }

    /// <summary>
    /// Size
    /// </summary>
    [DataMember(Name = "Size")]
    [Description("Size")]
    public required int Size { get; set; }

    /// <summary>
    /// Timestamp
    /// </summary>
    [DataMember(Name = "Timestamp")]
    [Description("Timestamp")]
    public required string Timestamp { get; set; }
}
