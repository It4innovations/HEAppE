using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.FileTransfer.Models;

/// <summary>
/// File upload result
/// </summary>
[DataContract(Name = "FileUploadResultExt")]
[Description("Result of upload of a single file")]
public class FileUploadResultExt
{
    /// <summary>
    /// FileName
    /// </summary>
    [DataMember(Name = "FileName")]
    [Description("Original file name")]
    public string FileName { get; set; }

    /// <summary>
    /// Succeeded
    /// </summary>
    [DataMember(Name = "Succeeded")]
    [Description("True if file upload succeeded, false otherwise")]
    public bool Succeeded { get; set; }

    /// <summary>
    /// Path
    /// </summary>
    [DataMember(Name = "Path")]
    [Description("Path where the file was uploaded")]
    public string Path { get; set; }

    public override string ToString()
    {
        return $"FileUploadResultExt(fileName={FileName}; succeeded={Succeeded}; path={Path})";
    }
}

/// <summary>
/// Job upload result
/// </summary>
[DataContract(Name = "FileUploadResultExt")]
[Description("Result of upload of a single job file")]
public class JobUploadResultExt : FileUploadResultExt
{

    /// <summary>
    /// AttributesSet
    /// </summary>
    [DataMember(Name = "AttributesSet")]
    [Description("True if attributess were set successfully, false in case of error, null if endpoint does not set them")]
    public bool? AttributesSet { get; set; } = null;

    public override string ToString()
    {
        return $"FileUploadResultExt(fileName={FileName}; succeeded={Succeeded}; path={Path}; attributesSet={AttributesSet})";
    }
}
