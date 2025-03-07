using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repositories;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Subject> Subjects { get; set; }
}
