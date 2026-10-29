using System.ComponentModel.DataAnnotations;

namespace IncidentReportingPortal.Models.Enums
{
    public enum IncidentCategory
    {
        [Display(Name = "Phishing")]
        Phishing = 1,

        [Display(Name = "Malware")]
        Malware = 2,

        [Display(Name = "Unauthorized Access")]
        UnauthorizedAccess = 3,

        [Display(Name = "Suspicious Login")]
        SuspiciousLogin = 4,

        [Display(Name = "Data Breach")]
        DataBreach = 5,

        [Display(Name = "Other")]
        Other = 6
    }
}
