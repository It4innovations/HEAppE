using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.ExtModels.DataTransfer.Models;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.DataTransfer;

/// <summary>
/// Model for sending HTTP POST request to job node
/// </summary>
[DataContract(Name = "HttpPostToJobNodeModel")]
[Description("Model for sending HTTP POST request to job node")]
public class HttpPostToJobNodeModel : SessionCodeModel
{
    /// <summary>
    /// Submitted task info id
    /// </summary>
    [DataMember(Name = "SubmittedTaskInfoId")]
    [Description("Submitted task info id")]
    public long SubmittedTaskInfoId { get; set; }

    /// <summary>
    /// Http request
    /// </summary>
    [DataMember(Name = "HttpRequest")]
    [Description("Http request")]
    public string HttpRequest { get; set; }

    /// <summary>
    /// Http headers
    /// </summary>
    [DataMember(Name = "HttpHeaders")]
    [Description("Http headers")]
    public IEnumerable<HTTPHeaderExt> HttpHeaders { get; set; }

    /// <summary>
    /// Http payload
    /// </summary>
    [DataMember(Name = "HttpPayload")]
    [Description("Http payload")]
    public string HttpPayload { get; set; }

    /// <summary>
    /// Node ip address
    /// </summary>
    [DataMember(Name = "nodeIPAddress")]
    [StringLength(50)]
    [Description("Node ip address")]
    public string NodeIPAddress { get; set; }

    /// <summary>
    /// Node port
    /// </summary>
    [DataMember(Name = "NodePort")]
    [Required]
    [Description("Node port")]
    public int NodePort { get; set; }

    public override string ToString()
    {
        return
            $"HttpPostToJobNodeModel({base.ToString()}; SubmittedTaskInfoId: {SubmittedTaskInfoId}); HttpRequest: {HttpRequest}; HttpHeaders: {string.Join("; ", HttpHeaders)}; HttpPayload: {HttpPayload}; NodeIPAddress: {NodeIPAddress}; NodePort: {NodePort})";
    }
}
