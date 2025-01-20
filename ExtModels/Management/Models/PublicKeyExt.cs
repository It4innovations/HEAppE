using HEAppE.ExtModels.FileTransfer.Models;
using System.ComponentModel;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Public key ext
/// </summary>
[Description("Public key ext")]
public class PublicKeyExt
{
    /// <summary>
    /// Username
    /// </summary>
    [Description("Username")] 
    public string Username { get; set; }

    /// <summary>
    /// Key type
    /// </summary>
    [Description("Key type")] 
    public FileTransferCipherTypeExt KeyType { get; set; }

    /// <summary>
    /// Public key OpenSSH
    /// </summary>
    [Description("Public key OpenSSH")] 
    public string PublicKeyOpenSSH { get; set; }

    /// <summary>
    /// Public key PEM
    /// </summary>
    [Description("Public key PEM")] 
    public string PublicKeyPEM { get; set; }
}