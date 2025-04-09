using System;
using System.Security.Cryptography;

namespace HEAppE.CertificateGenerator.Generators;

/// <summary>
///     Generic SSH Cipher
/// </summary>
public abstract class GenericCertGenerator : IDisposable
{
    #region IDisposable Members

    /// <summary>
    ///     Dispose
    /// </summary>
    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Instances

    /// <summary>
    ///     Key
    /// </summary>
    protected AsymmetricAlgorithm _key = null;

    /// <summary>
    ///     Public comment
    /// </summary>
    protected string _publicComment = $"key-temp-added-{DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss")}";

    #endregion

    #region Abstract Methods

    /// <summary>
    ///     Re-Generate key
    /// </summary>
    public abstract void Regenerate();

    /// <summary>
    ///     Returns the SSH private key
    /// </summary>
    /// <returns></returns>
    public abstract string ToPrivateKey();

    /// <summary>
    ///     Returns the SSH public key
    /// </summary>
    /// <returns></returns>
    public abstract string ToPublicKey();

    /// <summary>
    ///     Returns the SSH public key in PuTTY format
    /// </summary>
    /// <returns></returns>
    public abstract string ToPuTTYPublicKey();

    #endregion

    #region Private Methods

    /// <summary>
    ///     Convert integer to byte[]
    /// </summary>
    /// <param name="number">Value</param>
    /// <returns></returns>
    protected static byte[] ToBytes(int number)
    {
        var bts = BitConverter.GetBytes(number);
        if (BitConverter.IsLittleEndian) Array.Reverse(bts);

        return bts;
    }

    /// <summary>
    ///     Dispose
    /// </summary>
    /// <param name="disposing">Value</param>
    protected void Dispose(bool disposing)
    {
        if (disposing) _key?.Dispose();
    }

    #endregion
}