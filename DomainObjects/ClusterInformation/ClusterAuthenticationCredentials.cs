using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DomainObjects.ClusterInformation
{
    [Table("ClusterAuthenticationCredentials")]
    public partial class ClusterAuthenticationCredentials : IdentifiableDbEntity
    {

        #region Properties
        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        //VAULT

        private ClusterProjectCredentialVaultPart _vaultData = ClusterProjectCredentialVaultPart.Empty;
        
        public bool IsVaultDataLoaded => _vaultData != ClusterProjectCredentialVaultPart.Empty;

        [StringLength(50)]
        public string Password
        {
            get { return _vaultData.Password; }
            set { _vaultData = _vaultData with { Password = value }; }
        }

        public string PrivateKey
        {
            get { return _vaultData.PrivateKey; }
            set { _vaultData = _vaultData with { PrivateKey = value }; }
        }

        public string PrivateKeyCertificate
        {
            get { return _vaultData.PrivateKeyCertificate; }
            set { _vaultData = _vaultData with { PrivateKeyCertificate = value }; }
        }

        [StringLength(50)]
        public string PrivateKeyPassphrase
        {
            get { return _vaultData.PrivateKeyPassword; }
            set { _vaultData = _vaultData with { PrivateKeyPassword = value }; }
        }

        //VAULT
        [Required]
        public ClusterAuthenticationCredentialsAuthType AuthenticationType { get; set; }


        [Required]
        public FileTransferCipherType CipherType { get; set; }



        [StringLength(200)]
        public string PublicKeyFingerprint { get; set; }

        [StringLength(200)]
        public string PublicKey { get; set; }
        [Required]
        public bool IsGenerated { get; set; } = false;

        [Required]
        public bool IsDeleted { get; set; } = false;

        public virtual List<ClusterProjectCredential> ClusterProjectCredentials { get; set; } = new List<ClusterProjectCredential>();
        #endregion
        #region Override Methods
        public override string ToString()
        {
            return $"ClusterAuthenticationCredentials: Username={Username}, AuthenticationType={AuthenticationType}, CipherType={CipherType}";
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
            if (_vaultData.Id != Id)
            {
                _vaultData = _vaultData with { Id = Id };
            }
            var base64PK = Convert.ToBase64String(Encoding.UTF8.GetBytes(PrivateKey.Replace("\r\n", "\n"))); // Replace CRLF with LF
            var base64PKCert = Convert.ToBase64String(Encoding.UTF8.GetBytes(PrivateKeyCertificate.Replace("\r\n", "\n"))); // Replace CRLF with LF
            var passphrase = PrivateKeyPassphrase;
            var password = Password;
            
            return withPrivateKeyEncode ? _vaultData with
            {
                PrivateKey = base64PK, 
                PrivateKeyCertificate = base64PKCert,
                PrivateKeyPassword = passphrase,
                Password = password
            } : _vaultData;
        }
        #endregion
    }
}