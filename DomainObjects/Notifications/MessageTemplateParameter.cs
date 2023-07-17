using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.Notifications
{
    [Table("MessageTemplateParameter")]
    public class MessageTemplateParameter : IdentifiableDbEntity
    {
        [Required]
        [StringLength(30)]
        public string Identifier { get; set; }

        [Required]
        [StringLength(500)]
        public string Query { get; set; }
    }
}