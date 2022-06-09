﻿namespace HEAppE.CertificateGenerator
{
    /// <summary>
    /// Cipher type
    /// </summary>
    public enum CipherType
    {
        Unknown = 1,
        RSA3072 = 2,
        RSA4096 = 3,
        nistP256 = 4,
        nistP521 = 5,
    }
}
