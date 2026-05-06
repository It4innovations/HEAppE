using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DomainObjects.FirecRest;

[Table("FirecRestEndpoint")]
public class FirecRestEndpoint : IdentifiableDbEntity
{
    #region Properties
    [Required] [StringLength(250)] 
    public string Name { get; set; }

    [Required] [StringLength(200)] 
    public string Description { get; set; }

    [Required] [StringLength(250)] 
    public string Url { get; set; }

    [StringLength(250)] 
    public string IdpUrl { get; set; }

    public virtual List<Cluster> Clusters { get; set; } = new();
    #endregion

    #region Override Methods
    public override string ToString()
    {
        return
            $"Cluster: Id={Id}, Name={Name}, Description={Description}, Url={Url}, IdpUrl={IdpUrl}";
    }
    #endregion
}