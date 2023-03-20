using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.OpenStack
{
    [Table("OpenStackProjectDomain")]
    public class OpenStackProjectDomain : IdentifiableDbEntity
    {
        [StringLength(40)]
        public string Name { get; set; }

        [StringLength(80)]
        public string UID { get; set; }

        public override string ToString()
        {
            return $"OpenStackProjectDomain: Id={Id}, Name={Name}, UID={UID}";
        }
    }
}
