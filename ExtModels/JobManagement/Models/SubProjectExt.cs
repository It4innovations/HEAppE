using System;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models;

[DataContract(Name = "SubProjectExt")]
public class SubProjectExt
{
    [DataMember(Name = "Id")] public long Id { get; set; }

    [DataMember(Name = "Identifier")] public string Identifier { get; set; }

    [DataMember(Name = "Description")] public string Description { get; set; }

    [DataMember(Name = "StartDate")] public DateTime StartDate { get; set; }

    [DataMember(Name = "EndDate")] public DateTime? EndDate { get; set; }

    [DataMember(Name = "ProjectId")] public long ProjectId { get; set; }

    public override string ToString()
    {
        return
            $"SubProjectExt(Id={Id}; Identifier={Identifier}; Description={Description}; StartDate={StartDate}; EndDate={EndDate}; ProjectId={ProjectId})";
    }
}