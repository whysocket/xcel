using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xcel.Services.Auth.Models;

namespace Infra.Repositories;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Qualification> Qualifications { get; set; }
    public DbSet<Level> Levels { get; set; }
    public DbSet<Person> Persons { get; set; }
    public DbSet<TutorApplication> TutorApplications { get; set; }
    public DbSet<TutorService> TutorServices { get; set; }
    public DbSet<TutorDocument> TutorDocuments { get; set; }
    public DbSet<TutorApplicationInterview> TutorApplicationInterviews { get; set; }
    public DbSet<FieldVersion> FieldVersions { get; set; }
    public DbSet<TutorProfile> TutorProfiles { get; set; }
    public DbSet<AvailabilityRule> AvailabilityRules { get; set; }

    // Xcel.Features.Auth - In the future will be migrated to an external service
    public DbSet<OtpEntity> Otps { get; set; }
    public DbSet<RoleEntity> Roles { get; set; }
    public DbSet<PersonRoleEntity> PersonRoles { get; set; }
    public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------- Person Mapping
        modelBuilder.Entity<Person>().HasIndex(p => p.EmailAddress).IsUnique();

        modelBuilder
            .Entity<Person>()
            .HasOne(p => p.TutorApplication)
            .WithOne(ta => ta.Applicant)
            .HasForeignKey<TutorApplication>(ta => ta.ApplicantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TutorApplication>().HasKey(ta => ta.Id);

        // ---------- PersonRoleEntity Mapping
        modelBuilder.Entity<PersonRoleEntity>().HasKey(pr => pr.Id);

        modelBuilder
            .Entity<PersonRoleEntity>()
            .HasOne(pr => pr.Person)
            .WithMany()
            .HasForeignKey(pr => pr.PersonId);

        modelBuilder
            .Entity<PersonRoleEntity>()
            .HasOne(pr => pr.Role)
            .WithMany(r => r.PersonRoles)
            .HasForeignKey(pr => pr.RoleId);

        modelBuilder
            .Entity<PersonRoleEntity>()
            .HasIndex(pr => new { pr.PersonId, pr.RoleId })
            .IsUnique();

        // ---------- RefreshTokenEntity Mapping
        modelBuilder
            .Entity<RefreshTokenEntity>()
            .HasOne(rt => rt.Person)
            .WithMany()
            .HasForeignKey(rt => rt.PersonId);
    }
}
