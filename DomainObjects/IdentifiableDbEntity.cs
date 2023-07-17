using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects
{
    public abstract class IdentifiableDbEntity : IComparable<IdentifiableDbEntity>
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public IdentifiableDbEntity() { }
        public IdentifiableDbEntity(IdentifiableDbEntity identifiableDbEntity)
        {

        }

        #region IComparable<IdentifiableDbEntity> Members

        public int CompareTo(IdentifiableDbEntity other)
        {
            return (int)(this.Id - other.Id);
        }

        #endregion
    }
}