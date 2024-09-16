namespace HEAppE.ExtModels.FileTransfer.Models
{
    public enum FileTransferCipherTypeExt
    {
        None = 0,
        RSA3072 = 1,
        RSA4096 = 2,
        nistP256 = 3,
        nistP521 = 4,
        Ed25519 = 5
    }
}
