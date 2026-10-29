using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IncidentReportingPortal.Models.Entities
{
    public class InvestigationNote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int IncidentId { get; set; }

        [ForeignKey(nameof(IncidentId))]
        public virtual Incident? Incident { get; set; }

        [Required]
        public string InvestigatorId { get; set; } = string.Empty;

        [ForeignKey(nameof(InvestigatorId))]
        public virtual ApplicationUser? Investigator { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Note { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
