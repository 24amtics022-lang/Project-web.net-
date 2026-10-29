using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace IncidentReportingPortal.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Department { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Incident> ReportedIncidents { get; set; } = new List<Incident>();
        public virtual ICollection<Incident> AssignedIncidents { get; set; } = new List<Incident>();
        public virtual ICollection<InvestigationNote> InvestigationNotes { get; set; } = new List<InvestigationNote>();
    }
}
