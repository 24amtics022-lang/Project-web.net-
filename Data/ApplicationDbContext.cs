using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using IncidentReportingPortal.Models.Entities;

namespace IncidentReportingPortal.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Incident> Incidents { get; set; }
        public DbSet<InvestigationNote> InvestigationNotes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure IncidentReference as unique index
            builder.Entity<Incident>()
                .HasIndex(i => i.IncidentReference)
                .IsUnique();

            // Additional indexes for dashboard and filtering performance
            builder.Entity<Incident>()
                .HasIndex(i => i.Status);

            builder.Entity<Incident>()
                .HasIndex(i => i.Severity);

            builder.Entity<Incident>()
                .HasIndex(i => i.Category);

            builder.Entity<Incident>()
                .HasIndex(i => i.CreatedAt);

            // Reporter relationship (Restrict delete so audit logs stay intact)
            builder.Entity<Incident>()
                .HasOne(i => i.ReportedByUser)
                .WithMany(u => u.ReportedIncidents)
                .HasForeignKey(i => i.ReportedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Assigned investigator relationship
            builder.Entity<Incident>()
                .HasOne(i => i.AssignedToUser)
                .WithMany(u => u.AssignedIncidents)
                .HasForeignKey(i => i.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Investigation notes relationships
            builder.Entity<InvestigationNote>()
                .HasOne(n => n.Incident)
                .WithMany(i => i.InvestigationNotes)
                .HasForeignKey(n => n.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<InvestigationNote>()
                .HasOne(n => n.Investigator)
                .WithMany(u => u.InvestigationNotes)
                .HasForeignKey(n => n.InvestigatorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
