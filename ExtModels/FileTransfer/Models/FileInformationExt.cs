using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.FileTransfer.Models;

/// <summary>
/// File information ext
/// </summary>
[DataContract(Name = "FileInformationExt")]
[Description("File information ext")]
public class FileInformationExt
{
    /// <summary>
    /// File name
    /// </summary>
    [DataMember(Name = "FileName")]
    [Description("File name")]
    public string FileName { get; set; }

    /// <summary>
    /// Last modified at date
    /// </summary>
    [DataMember(Name = "LastModifiedDate")]
    [Description("Last modified at date")]
    public DateTime? LastModifiedDate { get; set; }

    public override string ToString()
    {
        return $"FileInformationExt({base.ToString()}; FileName={FileName}; LastModifiedDate={LastModifiedDate})";
    }
}