using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xcel.Services.Auth.Models;

namespace Infra.Repositories;

public class PersonRole
{
    public Guid PersonId { get; set; }
    public Person Person { get; set; }

    public Guid RoleId { get; set; }
    public RoleEntity Role { get; set; }
}

internal class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Qualification> Qualifications { get; set; }
    public DbSet<Person> Persons { get; set; }
    public DbSet<Tutor> Tutors { get; set; }
    public DbSet<TutorService> TutorServices { get; set; }
    public DbSet<TutorDocument> TutorDocuments { get; set; }
    
    // Xcel.Services.Auth - In the future will be migrated to an external service
    public DbSet<OtpEntity> Otps { get; set; }
    public DbSet<RoleEntity> Roles { get; set; }
    public DbSet<PersonRole> PersonRoles { get; set; } // Add PersonRole DbSet

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PersonRole>()
            .HasKey(pr => new { pr.PersonId, pr.RoleId });

        modelBuilder.Entity<PersonRole>()
            .HasOne(pr => pr.Person)
            .WithMany()
            .HasForeignKey(pr => pr.PersonId);

        modelBuilder.Entity<PersonRole>()
            .HasOne(pr => pr.Role)
            .WithMany()
            .HasForeignKey(pr => pr.RoleId);
    }
}