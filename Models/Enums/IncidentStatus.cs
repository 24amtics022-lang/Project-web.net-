using System.ComponentModel.DataAnnotations;

namespace IncidentReportingPortal.Models.Enums
{
    public enum IncidentStatus
    {
        [Display(Name = "Submitted")]
        Submitted = 1,

        [Display(Name = "Under Investigation")]
        UnderInvestigation = 2,

        [Display(Name = "Resolved")]
        Resolved = 3,

        [Display(Name = "Closed")]
        Closed = 4
    }
}
