using HEAppE.ExtModels.DataTransfer.Models;
using HEAppE.RestApiModels.AbstractModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.DataTransfer
{
    [DataContract(Name = "HttpPostToJobNodeModel")]
    public class HttpPostToJobNodeModel : SubmittedJobInfoModel
    {
        [DataMember(Name = "HttpRequest")]
        public string HttpRequest { get; set; }

        [DataMember(Name = "HttpHeaders")]
        public IEnumerable<HTTPHeaderExt> HttpHeaders { get; set; }

        [DataMember(Name = "HttpPayload")]
        public string HttpPayload { get; set; }

        [DataMember(Name = "nodeIPAddress"), StringLength(50)]
        public string NodeIPAddress { get; set; }

        [DataMember(Name = "NodePort"), Required]
        public int NodePort { get; set; }
        public override string ToString()
        {
            return $"HttpPostToJobNodeModel({base.ToString()}; HttpRequest: {HttpRequest}; HttpHeaders: {String.Join("; ", HttpHeaders)}; HttpPayload: {HttpPayload}; NodeIPAddress: {NodeIPAddress}; NodePort: {NodePort})";
        }
    }
}
