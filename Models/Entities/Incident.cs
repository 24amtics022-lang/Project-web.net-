using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IncidentReportingPortal.Models.Enums;

namespace IncidentReportingPortal.Models.Entities
{
    public class Incident
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(32)]
        public string IncidentReference { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public IncidentCategory Category { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public IncidentSeverity Severity { get; set; }

        [Required]
        public IncidentStatus Status { get; set; } = IncidentStatus.Submitted;

        [Required]
        public DateTime IncidentDate { get; set; }

        // Reporter Foreign Key
        [Required]
        public string ReportedByUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(ReportedByUserId))]
        public virtual ApplicationUser? ReportedByUser { get; set; }

        // Optional Assigned Investigator Foreign Key
        public string? AssignedToUserId { get; set; }

        [ForeignKey(nameof(AssignedToUserId))]
        public virtual ApplicationUser? AssignedToUser { get; set; }

        // Optional secure evidence attachment
        [MaxLength(260)]
        public string? EvidenceFilePath { get; set; }

        [MaxLength(255)]
        public string? OriginalFileName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ResolvedAt { get; set; }

        // Navigation property for notes
        public virtual ICollection<InvestigationNote> InvestigationNotes { get; set; } = new List<InvestigationNote>();
    }
}
