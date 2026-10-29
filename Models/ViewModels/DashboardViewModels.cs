using IncidentReportingPortal.Models.Entities;
using IncidentReportingPortal.Models.Enums;

namespace IncidentReportingPortal.Models.ViewModels
{
    public class ReporterDashboardViewModel
    {
        public string ReporterName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int TotalReported { get; set; }
        public int OpenCount { get; set; }
        public int UnderInvestigationCount { get; set; }
        public int ResolvedCount { get; set; }
        public int ClosedCount { get; set; }
        public List<Incident> UserIncidents { get; set; } = new();
    }

    public class AdminDashboardViewModel
    {
        public int TotalIncidents { get; set; }
        public int OpenIncidents { get; set; }
        public int UnderInvestigationIncidents { get; set; }
        public int CriticalIncidents { get; set; }
        public int ResolvedIncidents { get; set; }
        public int ClosedIncidents { get; set; }

        public Dictionary<IncidentCategory, int> IncidentsByCategory { get; set; } = new();
        public Dictionary<IncidentSeverity, int> IncidentsBySeverity { get; set; } = new();
        public List<Incident> RecentIncidents { get; set; } = new();
    }
}
