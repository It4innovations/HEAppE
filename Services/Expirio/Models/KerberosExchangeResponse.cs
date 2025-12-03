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
    /// Ticket
    /// </summary>
    [DataMember(Name = "Ticket")]
    [Description("Ticket")]
    public string Ticket { get; set; }
}
