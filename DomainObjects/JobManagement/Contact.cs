using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement;

[Table("Contact")]
public class Contact : IdentifiableDbEntity, ISoftDeletableEntity
{
    [Required] [StringLength(255)] public string Email { get; set; }

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }

    public string PublicKey { get; set; } = null;

    public virtual List<ProjectContact> ProjectContacts { get; set; } = new();

    [Required] public bool IsDeleted { get; set; } = false;

    #region Public methods

    public override string ToString()
    {
        return
            $"Contact: Id={Id}, Email={Email}, IsDeleted={IsDeleted}, CreatedAt={CreatedAt}, ModifiedAt={ModifiedAt}";
    }

    #endregion
}