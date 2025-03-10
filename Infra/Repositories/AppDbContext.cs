﻿using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repositories;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Qualification> Qualifications { get; set; }
    public DbSet<Person> Persons { get; set; }
    public DbSet<Tutor> Tutors { get; set; }
    public DbSet<TutorService> TutorServices { get; set; }
    public DbSet<TutorDocument> TutorDocuments { get; set; }
}