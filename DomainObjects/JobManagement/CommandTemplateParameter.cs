using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement
{
    [Table("CommandTemplateParameter")]
    public class CommandTemplateParameter : IdentifiableDbEntity
    {
        [Required]
        [StringLength(20)]
        public string Identifier { get; set; }
        
        [StringLength(200)]
        public string Query { get; set; }
        
        [StringLength(200)]
        public string Description { get; set; }

        public bool IsVisible { get; set; } = true;

        [ForeignKey("CommandTemplate")]
        public long? CommandTemplateId { get; set; }
        public virtual CommandTemplate CommandTemplate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }

        public bool IsEnabled { get; set; } = true;
        public override string ToString()
        {
            return $"CommandTemplateParameter(Id: {Id}, Identifier:{Identifier}, Description: {Description}, Query: {Query}, IsVisible: {IsVisible}, CommandTemplateId: {CommandTemplateId}, CreatedAt: {CreatedAt}, ModifiedAt: {ModifiedAt})";
        }
    }
}