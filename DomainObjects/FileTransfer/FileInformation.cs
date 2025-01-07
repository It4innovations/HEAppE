using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.FileTransfer;

[NotMapped]
public class FileInformation
{
    public string FileName { get; set; }
    public DateTime? LastModifiedDate { get; set; }
}