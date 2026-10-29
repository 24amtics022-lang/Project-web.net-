using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IncidentReportingPortal.Data;
using IncidentReportingPortal.Models.Entities;
using IncidentReportingPortal.Models.Enums;
using IncidentReportingPortal.Models.ViewModels;

namespace IncidentReportingPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Admin/Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var totalIncidents = await _context.Incidents.CountAsync();
            var openIncidents = await _context.Incidents.CountAsync(i => i.Status == IncidentStatus.Submitted);
            var underInvestigation = await _context.Incidents.CountAsync(i => i.Status == IncidentStatus.UnderInvestigation);
            var criticalIncidents = await _context.Incidents.CountAsync(i => i.Severity == IncidentSeverity.Critical);
            var resolvedIncidents = await _context.Incidents.CountAsync(i => i.Status == IncidentStatus.Resolved);
            var closedIncidents = await _context.Incidents.CountAsync(i => i.Status == IncidentStatus.Closed);

            // Group by category
            var categoryData = await _context.Incidents
                .GroupBy(i => i.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.Category, v => v.Count);

            // Group by severity
            var severityData = await _context.Incidents
                .GroupBy(i => i.Severity)
                .Select(g => new { Severity = g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.Severity, v => v.Count);

            // Recent 6 incidents
            var recentIncidents = await _context.Incidents
                .Include(i => i.ReportedByUser)
                .OrderByDescending(i => i.CreatedAt)
                .Take(6)
                .ToListAsync();

            var viewModel = new AdminDashboardViewModel
            {
                TotalIncidents = totalIncidents,
                OpenIncidents = openIncidents,
                UnderInvestigationIncidents = underInvestigation,
                CriticalIncidents = criticalIncidents,
                ResolvedIncidents = resolvedIncidents,
                ClosedIncidents = closedIncidents,
                IncidentsByCategory = categoryData,
                IncidentsBySeverity = severityData,
                RecentIncidents = recentIncidents
            };

            return View(viewModel);
        }

        // GET: /Admin/Incidents (Incident Management Table with Filters & Pagination)
        [HttpGet]
        public async Task<IActionResult> Incidents(IncidentFilterViewModel filter)
        {
            var query = _context.Incidents
                .Include(i => i.ReportedByUser)
                .Include(i => i.AssignedToUser)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                var search = filter.SearchQuery.Trim().ToLower();
                query = query.Where(i =>
                    i.Title.ToLower().Contains(search) ||
                    i.IncidentReference.ToLower().Contains(search) ||
                    (i.ReportedByUser != null && i.ReportedByUser.FullName.ToLower().Contains(search)));
            }

            // Category filter
            if (filter.Category.HasValue)
            {
                query = query.Where(i => i.Category == filter.Category.Value);
            }

            // Severity filter
            if (filter.Severity.HasValue)
            {
                query = query.Where(i => i.Severity == filter.Severity.Value);
            }

            // Status filter
            if (filter.Status.HasValue)
            {
                query = query.Where(i => i.Status == filter.Status.Value);
            }

            // Date filters
            if (filter.StartDate.HasValue)
            {
                query = query.Where(i => i.IncidentDate >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(i => i.IncidentDate <= filter.EndDate.Value.AddDays(1).AddTicks(-1));
            }

            var totalCount = await query.CountAsync();
            var pageSize = filter.PageSize > 0 ? filter.PageSize : 10;
            var pageNumber = filter.PageNumber > 0 ? filter.PageNumber : 1;

            var incidents = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            filter.TotalCount = totalCount;
            filter.PageNumber = pageNumber;
            filter.PageSize = pageSize;
            filter.Incidents = incidents;

            return View(filter);
        }

        // GET: /Admin/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var incident = await _context.Incidents
                .Include(i => i.ReportedByUser)
                .Include(i => i.AssignedToUser)
                .Include(i => i.InvestigationNotes.OrderByDescending(n => n.CreatedAt))
                    .ThenInclude(n => n.Investigator)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (incident == null)
            {
                return NotFound();
            }

            var investigators = await _userManager.GetUsersInRoleAsync("Admin");

            var viewModel = new IncidentDetailsViewModel
            {
                Incident = incident,
                IsAdmin = true,
                CanEditOrWithdraw = true,
                NewStatus = incident.Status,
                NewSeverity = incident.Severity,
                NewAssignedToUserId = incident.AssignedToUserId,
                AvailableInvestigators = investigators.ToList()
            };

            return View(viewModel);
        }

        // POST: /Admin/UpdateIncident/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateIncident(int id, IncidentStatus newStatus, IncidentSeverity newSeverity, string? newAssignedToUserId)
        {
            var incident = await _context.Incidents.FirstOrDefaultAsync(i => i.Id == id);
            if (incident == null)
            {
                return NotFound();
            }

            var oldStatus = incident.Status;
            var oldSeverity = incident.Severity;
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            incident.Status = newStatus;
            incident.Severity = newSeverity;
            incident.AssignedToUserId = string.IsNullOrWhiteSpace(newAssignedToUserId) ? null : newAssignedToUserId;
            incident.UpdatedAt = DateTime.UtcNow;

            if (newStatus == IncidentStatus.Resolved && oldStatus != IncidentStatus.Resolved)
            {
                incident.ResolvedAt = DateTime.UtcNow;
            }
            else if (newStatus != IncidentStatus.Resolved)
            {
                incident.ResolvedAt = null;
            }

            // Log status transition note automatically
            if (oldStatus != newStatus || oldSeverity != newSeverity)
            {
                var auditNote = new InvestigationNote
                {
                    IncidentId = incident.Id,
                    InvestigatorId = adminUserId,
                    Note = $"[System Audit] Status updated from '{oldStatus}' to '{newStatus}'. Severity updated from '{oldSeverity}' to '{newSeverity}'.",
                    CreatedAt = DateTime.UtcNow
                };
                _context.InvestigationNotes.Add(auditNote);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Incident {incident.IncidentReference} has been successfully updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Admin/AddNote/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(int incidentId, string noteText)
        {
            if (string.IsNullOrWhiteSpace(noteText))
            {
                TempData["ErrorMessage"] = "Investigation note cannot be empty.";
                return RedirectToAction(nameof(Details), new { id = incidentId });
            }

            var incident = await _context.Incidents.FindAsync(incidentId);
            if (incident == null)
            {
                return NotFound();
            }

            var investigatorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var note = new InvestigationNote
            {
                IncidentId = incidentId,
                InvestigatorId = investigatorId,
                Note = noteText.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.InvestigationNotes.Add(note);
            incident.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Investigation note added successfully.";
            return RedirectToAction(nameof(Details), new { id = incidentId });
        }

        // POST: /Admin/DeleteIncident/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIncident(int id)
        {
            var incident = await _context.Incidents.FindAsync(id);
            if (incident == null)
            {
                return NotFound();
            }

            var refNumber = incident.IncidentReference;
            _context.Incidents.Remove(incident);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Incident {Ref} deleted by Admin {AdminId}", refNumber, User.Identity?.Name);

            TempData["SuccessMessage"] = $"Incident {refNumber} was permanently deleted.";
            return RedirectToAction(nameof(Incidents));
        }
    }
}
