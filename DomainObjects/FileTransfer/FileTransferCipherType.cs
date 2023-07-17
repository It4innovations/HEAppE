namespace HEAppE.DomainObjects.FileTransfer
{
    /// <summary>
    /// Cipher type
    /// </summary>
    public enum FileTransferCipherType
    {
        Unknown = 1,
        RSA3072 = 2,
        RSA4096 = 3,
        nistP256 = 4,
        nistP521 = 5,
        Ed25519 = 6,
    }
}
