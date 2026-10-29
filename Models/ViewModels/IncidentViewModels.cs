using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using IncidentReportingPortal.Models.Entities;
using IncidentReportingPortal.Models.Enums;

namespace IncidentReportingPortal.Models.ViewModels
{
    public class IncidentCreateViewModel
    {
        [Required(ErrorMessage = "Incident title is required")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
        [Display(Name = "Incident Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select an incident category")]
        [Display(Name = "Category")]
        public IncidentCategory Category { get; set; } = IncidentCategory.Phishing;

        [Required(ErrorMessage = "Please specify the severity assessment")]
        [Display(Name = "Severity Level")]
        public IncidentSeverity Severity { get; set; } = IncidentSeverity.Medium;

        [Required(ErrorMessage = "Date and time of incident is required")]
        [Display(Name = "Date & Time of Occurrence")]
        [DataType(DataType.DateTime)]
        public DateTime IncidentDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Detailed description is required")]
        [StringLength(5000, MinimumLength = 15, ErrorMessage = "Description must be at least 15 characters long to assist the investigation")]
        [Display(Name = "Detailed Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Evidence Attachment (Optional: screenshot, log, PDF - Max 5MB)")]
        public IFormFile? EvidenceFile { get; set; }
    }

    public class IncidentDetailsViewModel
    {
        public Incident Incident { get; set; } = null!;
        public bool IsAdmin { get; set; }
        public bool CanEditOrWithdraw { get; set; }

        // Fields for Admin updates
        [Display(Name = "Update Status")]
        public IncidentStatus NewStatus { get; set; }

        [Display(Name = "Update Severity")]
        public IncidentSeverity NewSeverity { get; set; }

        [Display(Name = "Assign Investigator")]
        public string? NewAssignedToUserId { get; set; }

        [Display(Name = "Add Investigation Note")]
        [StringLength(2000, ErrorMessage = "Note cannot exceed 2000 characters")]
        public string? NewNote { get; set; }

        public List<ApplicationUser> AvailableInvestigators { get; set; } = new();
    }

    public class IncidentFilterViewModel
    {
        public string? SearchQuery { get; set; }
        public IncidentCategory? Category { get; set; }
        public IncidentSeverity? Severity { get; set; }
        public IncidentStatus? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        public List<Incident> Incidents { get; set; } = new();
    }
}
