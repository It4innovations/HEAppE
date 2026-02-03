using System.ComponentModel;
using System.Runtime.Serialization;

namespace Services.Expirio.Models;

/// <summary>
/// Exchange Request
/// </summary>
[DataContract(Name = "ExchangeRequest")]
[Description("Exchange request")]
public class ExchangeRequest
{
    /// <summary>
    /// Client name
    /// </summary>
    [DataMember(Name = "clientName", IsRequired = true)]
    [Description("Client name")]
    public required string ClientName { get; set; }

    /// <summary>
    /// Provider name
    /// </summary>
    [DataMember(Name = "providerName", IsRequired = true)]
    [Description("Provider name")]
    public required string ProviderName { get; set; }
}
