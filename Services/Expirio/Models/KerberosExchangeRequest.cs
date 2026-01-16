using System.ComponentModel;
using System.Runtime.Serialization;

namespace Services.Expirio.Models;

/// <summary>
/// Kerberos Exchange Request
/// </summary>
[DataContract(Name = "KerberosExchangeRequest")]
[Description("Kerberos exchange request")]
public class KerberosExchangeRequest
{
    /// <summary>
    /// Provider name
    /// </summary>
    [DataMember(Name = "ProviderName", IsRequired = true)]
    [Description("Provider name")]
    public required string ProviderName { get; set; }
}
