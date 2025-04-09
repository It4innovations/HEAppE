using System.ComponentModel;

namespace HEAppE.ExtModels.FileTransfer.Models;

/// <summary>
/// File tansfer cipher types
/// </summary>
[Description("File tansfer cipher types")]
public enum FileTransferCipherTypeExt
{
    None = 0,
    RSA3072 = 1,
    RSA4096 = 2,
    nistP256 = 3,
    nistP521 = 4,
    Ed25519 = 5
}