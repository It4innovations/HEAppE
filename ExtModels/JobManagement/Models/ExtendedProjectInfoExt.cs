using System;
using System.Runtime.Serialization;
using HEAppE.ExtModels.ClusterInformation.Models;

namespace HEAppE.ExtModels.JobManagement.Models;

[DataContract(Name = "ExtendedProjectInfoExt")]
public class ExtendedProjectInfoExt
{
    [DataMember(Name = "Id")] public long Id { get; set; }

    [DataMember(Name = "Name")] public string Name { get; set; }

    [DataMember(Name = "Description")] public string Description { get; set; }

    [DataMember(Name = "AccountingString")]
    public string AccountingString { get; set; }

    [DataMember(Name = "PrimaryInvestigatorContact")]
    public string PrimaryInvestigatorContact { get; set; }

    [DataMember(Name = "Contacts")] public string[] Contacts { get; set; }

    [DataMember(Name = "StartDate")] public DateTime StartDate { get; set; }

    [DataMember(Name = "EndDate")] public DateTime EndDate { get; set; }

    [DataMember(Name = "CommandTemplates")]
    public CommandTemplateExt[] CommandTemplates { get; set; }

    #region Public methods

    public override string ToString()
    {
        return
            $"Project: Id={Id}, Name={Name}, Description={Description}, AccountingString={AccountingString}, StartDate={StartDate}, EndDate={EndDate}, commandTemplates={CommandTemplates}";
    }

    #endregion
}