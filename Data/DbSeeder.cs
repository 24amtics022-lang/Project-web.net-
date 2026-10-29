using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IncidentReportingPortal.Models.Entities;
using IncidentReportingPortal.Models.Enums;

namespace IncidentReportingPortal.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndUsersAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Ensure database and schema are created
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Roles
            string[] roles = { "Admin", "Reporter" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Administrator
            var adminEmail = "admin@cyberportal.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Security Operations Center Admin",
                    Department = "Cyber Defense & Threat Intel",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                };

                var createAdminResult = await userManager.CreateAsync(adminUser, "Admin@12345Cyber!");
                if (createAdminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 3. Seed Demo Reporter
            var reporterEmail = "reporter@cyberportal.com";
            var reporterUser = await userManager.FindByEmailAsync(reporterEmail);
            if (reporterUser == null)
            {
                reporterUser = new ApplicationUser
                {
                    UserName = reporterEmail,
                    Email = reporterEmail,
                    FullName = "Jane Doe (Staff Reporter)",
                    Department = "Financial Systems",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                };

                var createReporterResult = await userManager.CreateAsync(reporterUser, "Reporter@12345!");
                if (createReporterResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(reporterUser, "Reporter");
                }
            }

            // 4. Seed initial realistic incidents if none exist
            if (!await context.Incidents.AnyAsync() && reporterUser != null && adminUser != null)
            {
                var incident1 = new Incident
                {
                    IncidentReference = "INC-20261015-9481",
                    Title = "Suspicious Executive Spear-Phishing Email with Malicious Link",
                    Category = IncidentCategory.Phishing,
                    Severity = IncidentSeverity.High,
                    Status = IncidentStatus.UnderInvestigation,
                    IncidentDate = DateTime.UtcNow.AddDays(-6),
                    Description = "Several accounting staff received a spoofed email claiming to be from the CFO requesting urgent wire transfer authorization. The email contained a shortened Bitly link redirecting to a fake Microsoft 365 credential harvesting page.",
                    ReportedByUserId = reporterUser.Id,
                    AssignedToUserId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-6),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                };

                var incident2 = new Incident
                {
                    IncidentReference = "INC-20261018-3204",
                    Title = "Unauthorized VPN Login Attempts Detected from External IP",
                    Category = IncidentCategory.SuspiciousLogin,
                    Severity = IncidentSeverity.Critical,
                    Status = IncidentStatus.UnderInvestigation,
                    IncidentDate = DateTime.UtcNow.AddDays(-3),
                    Description = "Over 140 failed SSH and VPN authentication attempts originating from IP 185.220.101.5 within 2 minutes targeting the internal payroll server.",
                    ReportedByUserId = reporterUser.Id,
                    AssignedToUserId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                };

                var incident3 = new Incident
                {
                    IncidentReference = "INC-20261010-1129",
                    Title = "Workstation Ransomware Alert Quarantined by EDR",
                    Category = IncidentCategory.Malware,
                    Severity = IncidentSeverity.High,
                    Status = IncidentStatus.Resolved,
                    IncidentDate = DateTime.UtcNow.AddDays(-11),
                    Description = "Endpoint detection software flagged and quarantined Trojan.Downloader on HR laptop 42 after an unauthorized attachment was downloaded from web mail.",
                    ReportedByUserId = reporterUser.Id,
                    AssignedToUserId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-11),
                    UpdatedAt = DateTime.UtcNow.AddDays(-8),
                    ResolvedAt = DateTime.UtcNow.AddDays(-8)
                };

                context.Incidents.AddRange(incident1, incident2, incident3);
                await context.SaveChangesAsync();

                // Add sample investigation notes
                var note1 = new InvestigationNote
                {
                    IncidentId = incident1.Id,
                    InvestigatorId = adminUser.Id,
                    Note = "Blocked phishing sender domain '@financial-auth-cfo.com' at firewall level. Extracted IOCs and submitted domain to abuse feed.",
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                };

                var note2 = new InvestigationNote
                {
                    IncidentId = incident2.Id,
                    InvestigatorId = adminUser.Id,
                    Note = "Firewall rate limiting triggered. Source IP range geo-blocked. Enforced mandatory 2FA reset for targeted payroll credentials.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                };

                var note3 = new InvestigationNote
                {
                    IncidentId = incident3.Id,
                    InvestigatorId = adminUser.Id,
                    Note = "Hard drive image preserved for forensic analysis. Workstation cleanly wiped and re-imaged with latest OS patches.",
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                };

                context.InvestigationNotes.AddRange(note1, note2, note3);
                await context.SaveChangesAsync();
            }
        }
    }
}
