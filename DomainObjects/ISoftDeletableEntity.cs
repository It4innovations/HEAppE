namespace HEAppE.DomainObjects;

public interface ISoftDeletableEntity
{
    public bool IsDeleted { get; set; }
}