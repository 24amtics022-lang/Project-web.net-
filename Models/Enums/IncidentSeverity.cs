using System.ComponentModel.DataAnnotations;

namespace IncidentReportingPortal.Models.Enums
{
    public enum IncidentSeverity
    {
        [Display(Name = "Low")]
        Low = 1,

        [Display(Name = "Medium")]
        Medium = 2,

        [Display(Name = "High")]
        High = 3,

        [Display(Name = "Critical")]
        Critical = 4
    }
}
