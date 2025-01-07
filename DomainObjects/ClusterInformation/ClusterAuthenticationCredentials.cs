using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Text;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DomainObjects.ClusterInformation;

[Table("ClusterAuthenticationCredentials")]
public class ClusterAuthenticationCredentials : IdentifiableDbEntity, ISoftDeletableEntity
{
    #region Properties

    [Required] [StringLength(50)] public string Username { get; set; }

    //VAULT

    private ClusterProjectCredentialVaultPart _vaultData = ClusterProjectCredentialVaultPart.Empty;

    public bool IsVaultDataLoaded => _vaultData != ClusterProjectCredentialVaultPart.Empty;

    [StringLength(50)]
    public string Password
    {
        get => _vaultData.Password;
        set => _vaultData = _vaultData with { Password = value };
    }

    public string PrivateKey
    {
        get => _vaultData.PrivateKey;
        set => _vaultData = _vaultData with { PrivateKey = value };
    }

    public string PrivateKeyCertificate
    {
        get => _vaultData.PrivateKeyCertificate;
        set => _vaultData = _vaultData with { PrivateKeyCertificate = value };
    }

    [StringLength(50)]
    public string PrivateKeyPassphrase
    {
        get => _vaultData.PrivateKeyPassword;
        set => _vaultData = _vaultData with { PrivateKeyPassword = value };
    }

    //VAULT
    [Required] public ClusterAuthenticationCredentialsAuthType AuthenticationType { get; set; }


    [Required] public FileTransferCipherType CipherType { get; set; }


    [StringLength(200)] public string PublicKeyFingerprint { get; set; }

    [StringLength(200)] public string PublicKey { get; set; }

    [Required] public bool IsGenerated { get; set; } = false;

    [Required] public bool IsDeleted { get; set; } = false;

    public virtual List<ClusterProjectCredential> ClusterProjectCredentials { get; set; } = new();

    #endregion

    #region Override Methods

    public override string ToString()
    {
        return
            $"ClusterAuthenticationCredentials: Username={Username}, AuthenticationType={AuthenticationType}, CipherType={CipherType}";
    }

    public void ImportVaultData(ClusterProjectCredentialVaultPart data)
    {
        var fromBase64PK = Encoding.UTF8.GetString(Convert.FromBase64String(data.PrivateKey));
        var fromBase64PKCert = Encoding.UTF8.GetString(Convert.FromBase64String(data.PrivateKeyCertificate));
        var passphrase = data.PrivateKeyPassword;
        var password = data.Password;
        _vaultData = data with
        {
            PrivateKey = fromBase64PK,
            PrivateKeyCertificate = fromBase64PKCert,
            PrivateKeyPassword = passphrase,
            Password = password
        };
    }

    public ClusterProjectCredentialVaultPart ExportVaultData(bool withPrivateKeyEncode = true)
    {
        if (_vaultData.Id != Id) _vaultData = _vaultData with { Id = Id };

        //if PrivateKey is path then cat file and encode
        if (Path.Exists(PrivateKey)) PrivateKey = File.ReadAllText(PrivateKey);

        var base64PK =
            Convert.ToBase64String(Encoding.UTF8.GetBytes(PrivateKey.Replace("\r\n", "\n"))); // Replace CRLF with LF
        var base64PKCert =
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes(PrivateKeyCertificate.Replace("\r\n", "\n"))); // Replace CRLF with LF
        var passphrase = PrivateKeyPassphrase;
        var password = Password;

        return withPrivateKeyEncode
            ? _vaultData with
            {
                PrivateKey = base64PK,
                PrivateKeyCertificate = base64PKCert,
                PrivateKeyPassword = passphrase,
                Password = password
            }
            : _vaultData;
    }

    #endregion
}