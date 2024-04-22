using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement;

[Table("ClusterNodeTypeAggregationAccounting")]
public class ClusterNodeTypeAggregationAccounting
{
    public long ClusterNodeTypeAggregationId { get; set; }
    public virtual ClusterNodeTypeAggregation ClusterNodeTypeAggregation { get; set; }
    
    public long AccountingId { get; set; }
    public virtual Accounting Accounting { get; set; }
}