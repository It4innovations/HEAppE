using System.ComponentModel;

namespace HEAppE.ExtModels.FileTransfer.Models;

/// <summary>
/// File tansfer cipher types
/// </summary>
[Description("File tansfer cipher types")]
public enum FileTransferCipherTypeExt
{
    None = 1,
    RSA3072 = 2,
    RSA4096 = 3,
    nistP256 = 4,
    nistP521 = 5,
    Ed25519 = 6
}