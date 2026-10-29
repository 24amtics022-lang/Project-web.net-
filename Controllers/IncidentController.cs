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
    [Authorize]
    public class IncidentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<IncidentController> _logger;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf", ".txt", ".log" };
        private const long MaxFileSizeInBytes = 5 * 1024 * 1024; // 5 MB

        public IncidentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            ILogger<IncidentController> logger)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
        }

        // GET: /Incident/Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);

            var incidents = await _context.Incidents
                .Where(i => i.ReportedByUserId == userId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var viewModel = new ReporterDashboardViewModel
            {
                ReporterName = user?.FullName ?? User.Identity?.Name ?? "Reporter",
                Department = user?.Department ?? "General",
                TotalReported = incidents.Count,
                OpenCount = incidents.Count(i => i.Status == IncidentStatus.Submitted),
                UnderInvestigationCount = incidents.Count(i => i.Status == IncidentStatus.UnderInvestigation),
                ResolvedCount = incidents.Count(i => i.Status == IncidentStatus.Resolved),
                ClosedCount = incidents.Count(i => i.Status == IncidentStatus.Closed),
                UserIncidents = incidents
            };

            return View(viewModel);
        }

        // GET: /Incident/Create
        [HttpGet]
        public IActionResult Create()
        {
            var model = new IncidentCreateViewModel
            {
                IncidentDate = DateTime.Now
            };
            return View(model);
        }

        // POST: /Incident/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IncidentCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            string? savedRelativePath = null;
            string? originalFileName = null;

            // Secure File Upload Handling
            if (model.EvidenceFile != null && model.EvidenceFile.Length > 0)
            {
                if (model.EvidenceFile.Length > MaxFileSizeInBytes)
                {
                    ModelState.AddModelError("EvidenceFile", "File size cannot exceed 5 MB.");
                    return View(model);
                }

                var extension = Path.GetExtension(model.EvidenceFile.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("EvidenceFile", "Only .jpg, .jpeg, .png, .pdf, .txt, and .log files are permitted.");
                    return View(model);
                }

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                originalFileName = Path.GetFileName(model.EvidenceFile.FileName);
                var uniqueFileName = $"{Guid.NewGuid():N}_{originalFileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.EvidenceFile.CopyToAsync(stream);
                }

                savedRelativePath = $"/uploads/{uniqueFileName}";
            }

            // Generate Unique Reference Number: INC-YYYYMMDD-XXXX
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var randomPart = Guid.NewGuid().ToString("N")[..4].ToUpper();
            var incidentRef = $"INC-{datePart}-{randomPart}";

            var incident = new Incident
            {
                IncidentReference = incidentRef,
                Title = model.Title.Trim(),
                Category = model.Category,
                Severity = model.Severity,
                Status = IncidentStatus.Submitted,
                IncidentDate = model.IncidentDate,
                Description = model.Description.Trim(),
                ReportedByUserId = userId,
                EvidenceFilePath = savedRelativePath,
                OriginalFileName = originalFileName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Incidents.Add(incident);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Incident {Ref} created by User ID {UserId}", incident.IncidentReference, userId);

            TempData["SuccessMessage"] = $"Incident successfully reported! Your Tracking Reference ID is: {incident.IncidentReference}";
            return RedirectToAction(nameof(Dashboard));
        }

        // GET: /Incident/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

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

            // Strict Server-Side Access Control:
            // Only the owner of the report OR an Admin may view incident details.
            if (!isAdmin && incident.ReportedByUserId != userId)
            {
                _logger.LogWarning("Unauthorized access attempt: User {UserId} tried to view Incident {IncidentId}", userId, id);
                return Forbid();
            }

            var viewModel = new IncidentDetailsViewModel
            {
                Incident = incident,
                IsAdmin = isAdmin,
                CanEditOrWithdraw = (incident.ReportedByUserId == userId && incident.Status == IncidentStatus.Submitted),
                NewStatus = incident.Status,
                NewSeverity = incident.Severity,
                NewAssignedToUserId = incident.AssignedToUserId
            };

            return View(viewModel);
        }

        // POST: /Incident/Withdraw/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var incident = await _context.Incidents.FirstOrDefaultAsync(i => i.Id == id);

            if (incident == null)
            {
                return NotFound();
            }

            // Authorization check
            if (incident.ReportedByUserId != userId)
            {
                return Forbid();
            }

            // Business rule: Only submitted incidents that haven't been picked up for investigation can be withdrawn
            if (incident.Status != IncidentStatus.Submitted)
            {
                TempData["ErrorMessage"] = "Cannot withdraw an incident that is already under investigation or resolved.";
                return RedirectToAction(nameof(Details), new { id });
            }

            incident.Status = IncidentStatus.Closed;
            incident.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Incident {incident.IncidentReference} was successfully withdrawn and closed.";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
