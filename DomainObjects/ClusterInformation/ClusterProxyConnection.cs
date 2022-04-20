using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.ClusterInformation
{
    [Table("ClusterProxyConnection")]
    public class ClusterProxyConnection : IdentifiableDbEntity
    {
        #region Properties
        [Required]
        [StringLength(40)]
        public string Host { get; set; }

        [Required]
        public int Port { get; set; }

        [Required]
        public ProxyType Type { get; set; }

        [StringLength(50)]
        public string Username { get; set; }

        [StringLength(50)]
        public string Password { get; set; }

        #endregion
        #region Override Methods
        public override string ToString()
        {
            return $"ClusterProxyConnection: Id={Id}, Host={Host}, Username={Username}, Password={Password}, Port={Port}, Type={Type}";
        }
        #endregion
    }
}
