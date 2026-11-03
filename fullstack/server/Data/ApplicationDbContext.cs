using fullstackapplication.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace fullstackapplication.Server.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<EmployeeAccount> EmployeeAccounts => Set<EmployeeAccount>();

    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();

    public DbSet<CandidateWorkEntry> CandidateWorkEntries => Set<CandidateWorkEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var account = modelBuilder.Entity<EmployeeAccount>();
        account.HasKey(employee => employee.Id);
        account.HasIndex(employee => employee.NormalizedUsername).IsUnique();
        account.Property(employee => employee.Username).HasMaxLength(50).IsRequired();
        account.Property(employee => employee.NormalizedUsername).HasMaxLength(50).IsRequired();
        account.Property(employee => employee.DisplayName).HasMaxLength(100).IsRequired();
        account.Property(employee => employee.Role).HasMaxLength(20).IsRequired();
        account.Property(employee => employee.PasswordHash).HasMaxLength(500).IsRequired();
        account.Property(employee => employee.IsActive).IsRequired();
        account.Property(employee => employee.InactiveAtUtc);

        var profile = modelBuilder.Entity<CandidateProfile>();
        profile.HasKey(candidate => candidate.EmployeeAccountId);
        profile.Property(candidate => candidate.FirstName).HasMaxLength(50).IsRequired();
        profile.Property(candidate => candidate.LastName).HasMaxLength(50).IsRequired();
        profile.Property(candidate => candidate.Email).HasMaxLength(254).IsRequired();
        profile.Property(candidate => candidate.PhoneNumber).HasMaxLength(30);
        profile.Property(candidate => candidate.Experience).HasMaxLength(2000).IsRequired();
        profile.Property(candidate => candidate.Stack).HasMaxLength(200).IsRequired();
        profile.Property(candidate => candidate.PerformanceScore).IsRequired();
        profile.Property(candidate => candidate.ResumeContent).HasColumnType("varbinary(max)");
        profile.Property(candidate => candidate.ResumeFileName).HasMaxLength(255);
        profile.Property(candidate => candidate.ResumeContentType).HasMaxLength(100);
        profile.HasOne(candidate => candidate.EmployeeAccount)
            .WithOne(account => account.CandidateProfile)
            .HasForeignKey<CandidateProfile>(candidate => candidate.EmployeeAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        var workEntry = modelBuilder.Entity<CandidateWorkEntry>();
        workEntry.HasKey(entry => entry.Id);
        workEntry.Property(entry => entry.Task).HasMaxLength(200).IsRequired();
        workEntry.Property(entry => entry.HoursSpent).HasPrecision(4, 1).IsRequired();
        workEntry.Property(entry => entry.Accomplishment).HasMaxLength(2000).IsRequired();
        workEntry.Property(entry => entry.Learnings).HasMaxLength(2000).IsRequired();
        workEntry.Property(entry => entry.BlockersAndChallenges).HasMaxLength(2000).IsRequired();
        workEntry.Property(entry => entry.AiToolsUsed).HasMaxLength(500).IsRequired();
        workEntry.Property(entry => entry.AdminFeedback).HasMaxLength(2000);
        workEntry.HasIndex(entry => new { entry.CandidateProfileId, entry.WorkDate });
        workEntry.HasOne(entry => entry.CandidateProfile)
            .WithMany(profile => profile.WorkEntries)
            .HasForeignKey(entry => entry.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
