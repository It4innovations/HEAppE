using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects;

public abstract class IdentifiableDbEntity : IComparable<IdentifiableDbEntity>
{
    public IdentifiableDbEntity()
    {
    }

    public IdentifiableDbEntity(IdentifiableDbEntity identifiableDbEntity)
    {
    }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    #region IComparable<IdentifiableDbEntity> Members

    public int CompareTo(IdentifiableDbEntity other)
    {
        return (int)(Id - other.Id);
    }

    #endregion
}