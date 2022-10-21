﻿using HEAppE.ExtModels.JobManagement.Models;
using System;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models
{
    [DataContract(Name = "CommandTemplateExt")]
    public class CommandTemplateExt
    {
        [DataMember(Name = "Id")]
        public long? Id { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "Project")]
        public ProjectExt Project { get; set;}

        [DataMember(Name = "Description")]
        public string Description { get; set; }

        [DataMember(Name = "Code")]
        public string Code { get; set; }

        [DataMember(Name = "IsGeneric")]
        public bool IsGeneric { get; set; }

        [DataMember(Name = "TemplateParameters")]
        public CommandTemplateParameterExt[] TemplateParameters { get; set; }

        public override string ToString()
        {
            return $"CommandTemplateExt(id={Id}; name={Name}; description={Description}; code={Code}; isGeneric={IsGeneric}; templateParameters={TemplateParameters})";
        }
    }
}
