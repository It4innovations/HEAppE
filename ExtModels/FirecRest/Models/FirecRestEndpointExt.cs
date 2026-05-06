using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.FirecRest.Models;

/// <summary>
/// FirecRestEndpointExt
/// </summary>
[DataContract(Name = "FirecRestEndpointExt")]
[Description("FirecRestEndpointExt")]
public class FirecRestEndpointExt
{
    [DataMember(Name = "Id")]
    [Description("Id")]
    public long? Id { get; set; }

    [DataMember(Name = "Name")]
    [Description("Name")]
    public string Name { get; set; }

    [DataMember(Name = "Description")]
    [Description("Description")]
    public string Description { get; set; }

    [DataMember(Name = "Url")]
    [Description("FirecREST Url")]
    public string Url { get; set; }

    [DataMember(Name = "IdpUrl")]
    [Description("Keycloak url")]
    public string IdpUrl { get; set; }

    public override string ToString()
    {
        return $"FirecRestEndpointExt(Id={Id}; Name={Name}; Description={Description}; Url={Url}; IdpUrl={IdpUrl})";
    }
}