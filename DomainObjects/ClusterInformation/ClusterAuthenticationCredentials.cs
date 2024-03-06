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

        [Required]
        public ClusterAuthenticationCredentialsAuthType AuthenticationType { get; set; }


        [Required]
        public FileTransferCipherType CipherType { get; set; }


        //VAULT

        [StringLength(200)]
        public string PublicKeyFingerprint { get; set; }
        [Required]
        public bool IsGenerated { get; set; } = false;

        [Required]
        public bool IsDeleted { get; set; } = false;

        public virtual List<ClusterProjectCredential> ClusterProjectCredentials { get; set; } = new List<ClusterProjectCredential>();
        #endregion
        #region Override Methods
        public override string ToString()
        {
            return $"ClusterAuthenticationCredentials: Username={Username}, AuthenticationType={nameof(AuthenticationType)}, CipherType={CipherType}";
        }

        public void ImportVaultData(ClusterProjectCredentialVaultPart data)
        {
            var fromBase64PK = Encoding.UTF8.GetString(Convert.FromBase64String(data.PrivateKey));
            _vaultData = data with { PrivateKey = fromBase64PK };
        }

        public ClusterProjectCredentialVaultPart ExportVaultData(bool withPrivateKeyEncode = true)
        {
            if (_vaultData.Id != Id)
            {
                _vaultData = _vaultData with { Id = Id };
            }
            var base64PK = Convert.ToBase64String(Encoding.UTF8.GetBytes(PrivateKey.Replace("\r\n", "\n"))); // Replace CRLF with LF
            return withPrivateKeyEncode ? _vaultData with { PrivateKey = base64PK } : _vaultData;
        }
        #endregion
    }
}