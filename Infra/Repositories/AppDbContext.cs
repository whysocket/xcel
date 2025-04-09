using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xcel.Services.Auth.Models;

namespace Infra.Repositories;

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
    public DbSet<PersonRoleEntity> PersonRoles { get; set; }
    public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }
    
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PersonRoleEntity>()
            .HasKey(pr => pr.Id);

        modelBuilder.Entity<PersonRoleEntity>()
            .HasOne(pr => pr.Person)
            .WithMany()
            .HasForeignKey(pr => pr.PersonId);

        modelBuilder.Entity<PersonRoleEntity>()
            .HasOne(pr => pr.Role)
            .WithMany(r => r.PersonRoles)
            .HasForeignKey(pr => pr.RoleId);

        modelBuilder.Entity<PersonRoleEntity>()
            .HasIndex(pr => new { pr.PersonId, pr.RoleId })
            .IsUnique();
        
        modelBuilder.Entity<RefreshTokenEntity>()
            .HasOne(rt => rt.Person)
            .WithMany()
            .HasForeignKey(rt => rt.PersonId);
    }
}