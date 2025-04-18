using Domain.Entities;
using Infra.Repositories;
using Microsoft.EntityFrameworkCore;
using Xcel.Services.Auth.Constants;
using Xcel.Services.Auth.Models;

namespace Infra.Extensions.DbContext;

internal static class SeedExtensions
{
    internal static async Task SeedAsync(this AppDbContext db)
    {
        var utcNow = DateTime.UtcNow;

        // ---- Roles ----
        if (!await db.Roles.AnyAsync())
        {
            var roles = new[] { UserRoles.Admin, UserRoles.Moderator, UserRoles.Reviewer };
            await db.Roles.AddRangeAsync(roles.Select(r => new RoleEntity { Name = r }));
            await db.SaveChangesAsync();
        }

        var reviewerRole = await db.Roles.FirstAsync(r => r.Name == UserRoles.Reviewer);

        // ---- Persons ----
        var tutorPerson = await EnsurePersonAsync(db, "Ana", "Lata", "ana@xceltutors.com");
        var reviewerPerson = await EnsurePersonAsync(db, "Reviewer", "One", "reviewer@xceltutors.com", reviewerRole.Id);
        var tutor = await EnsurePersonAsync(db, "Jake", "Doe", "jake@xceltutors.com");

        if (!await db.TutorApplications.AnyAsync(x => x.ApplicantId == tutor.Id))
        {
            var app = new TutorApplication
            {
                Id = Guid.NewGuid(),
                ApplicantId = tutor.Id,
                CurrentStep = TutorApplication.OnboardingStep.DocumentsRequested
            };

            db.TutorApplications.Add(app);

            // Approved CV (1 version)
            db.TutorDocuments.Add(new TutorDocument
            {
                Id = Guid.NewGuid(),
                TutorApplicationId = app.Id,
                DocumentType = TutorDocument.TutorDocumentType.Cv,
                DocumentPath = "/seed/jake_cv_v1.pdf",
                Status = TutorDocument.TutorDocumentStatus.Approved,
                Version = 1
            });

            // ID document with 3 versions
            db.TutorDocuments.AddRange(new[]
            {
                new TutorDocument
                {
                    Id = Guid.NewGuid(),
                    TutorApplicationId = app.Id,
                    DocumentType = TutorDocument.TutorDocumentType.Id,
                    DocumentPath = "/seed/jake_id_v1.pdf",
                    Status = TutorDocument.TutorDocumentStatus.Pending,
                    Version = 1
                },
                new TutorDocument
                {
                    Id = Guid.NewGuid(),
                    TutorApplicationId = app.Id,
                    DocumentType = TutorDocument.TutorDocumentType.Id,
                    DocumentPath = "/seed/jake_id_v2.pdf",
                    Status = TutorDocument.TutorDocumentStatus.Pending,
                    Version = 2
                },
                new TutorDocument
                {
                    Id = Guid.NewGuid(),
                    TutorApplicationId = app.Id,
                    DocumentType = TutorDocument.TutorDocumentType.Id,
                    DocumentPath = "/seed/jake_id_v3.pdf",
                    Status = TutorDocument.TutorDocumentStatus.ResubmissionNeeded,
                    Version = 3,
                    ModeratorReason = "Image was too blurry"
                }
            });

            await db.SaveChangesAsync();
        }
        
        // ---- Subjects & Qualifications ----
        if (!await db.Subjects.AnyAsync())
        {
            var math = new Subject { Name = "Math" };
            var physics = new Subject { Name = "Physics" };
            await db.Subjects.AddRangeAsync(math, physics);
            await db.SaveChangesAsync();

            var qualifications = new[]
            {
                new Qualification { Name = "BSc Mathematics", SubjectId = math.Id },
                new Qualification { Name = "A-Level Physics", SubjectId = physics.Id }
            };
            await db.Qualifications.AddRangeAsync(qualifications);
            await db.SaveChangesAsync();
        }

        var qualification = await db.Qualifications.FirstAsync();

        // ---- TutorApplication ----
        if (!await db.TutorApplications.AnyAsync())
        {
            var app = new TutorApplication
            {
                Id = Guid.NewGuid(),
                ApplicantId = tutorPerson.Id,
                CurrentStep = TutorApplication.OnboardingStep.DocumentsRequested,
                IsRejected = false
            };

            db.TutorApplications.Add(app);

            // ---- Documents ----
            db.TutorDocuments.AddRange(new[]
            {
                new TutorDocument
                {
                    Id = Guid.NewGuid(),
                    DocumentType = TutorDocument.TutorDocumentType.Cv,
                    DocumentPath = "/seed/cv.pdf",
                    Status = TutorDocument.TutorDocumentStatus.Approved,
                    TutorApplicationId = app.Id
                },
                new TutorDocument
                {
                    Id = Guid.NewGuid(),
                    DocumentType = TutorDocument.TutorDocumentType.Id,
                    DocumentPath = "/seed/id.pdf",
                    Status = TutorDocument.TutorDocumentStatus.Approved,
                    TutorApplicationId = app.Id
                },
                new TutorDocument
                {
                    Id = Guid.NewGuid(),
                    DocumentType = TutorDocument.TutorDocumentType.Dbs,
                    DocumentPath = "/seed/dbs.pdf",
                    Status = TutorDocument.TutorDocumentStatus.Approved,
                    TutorApplicationId = app.Id
                }
            });

            // ---- Interview ----
            var interview = new TutorApplicationInterview
            {
                Id = Guid.NewGuid(),
                Platform = TutorApplicationInterview.InterviewPlatform.GoogleMeets,
                Status = TutorApplicationInterview.InterviewStatus.Confirmed,
                ProposedDates = new List<DateTime> { utcNow.AddDays(1), utcNow.AddDays(2) },
                ScheduledAt = utcNow.AddDays(1),
                ReviewerId = reviewerPerson.Id,
                Reviewer = reviewerPerson,
                ConfirmedBy = reviewerPerson.Id,
                TutorApplicationId = app.Id
            };

            app.Interview = interview;

            // ---- TutorProfile ----
            var profile = new TutorProfile
            {
                Id = Guid.NewGuid(),
                PersonId = tutorPerson.Id,
                Status = TutorProfile.TutorProfileStatus.PendingConfiguration,
                FieldVersions = new List<FieldVersion>
                {
                    new FieldVersion
                    {
                        FieldType = FieldVersion.Field.SessionBio,
                        Value = "Helping students reach their full potential.",
                        CreatedAt = utcNow,
                        Status = FieldStatus.Pending
                    },
                    new FieldVersion
                    {
                        FieldType = FieldVersion.Field.FullBio,
                        Value = "I have 5 years of tutoring experience in Math and Physics.",
                        CreatedAt = utcNow,
                        Status = FieldStatus.Pending
                    },
                    new FieldVersion
                    {
                        FieldType = FieldVersion.Field.CardBio,
                        Value = "Experienced tutor in Math and Physics.",
                        CreatedAt = utcNow,
                        Status = FieldStatus.Pending
                    }
                },
                TutorServices = new List<TutorService>
                {
                    new TutorService
                    {
                        QualificationId = qualification.Id,
                        PricePerHour = 30,
                        Status = TutorService.ServiceStatus.Pending
                    }
                }
            };

            db.TutorProfiles.Add(profile);
            await db.SaveChangesAsync();
        }
    }

    private static async Task<Person> EnsurePersonAsync(AppDbContext db, string first, string last, string email, Guid? roleId = null)
    {
        var person = await db.Persons.FirstOrDefaultAsync(p => p.EmailAddress == email);
        if (person != null) return person;

        var newPerson = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = first,
            LastName = last,
            EmailAddress = email
        };

        db.Persons.Add(newPerson);
        await db.SaveChangesAsync();

        if (roleId.HasValue)
        {
            db.Add(new PersonRoleEntity
            {
                PersonId = newPerson.Id,
                RoleId = roleId.Value
            });
            await db.SaveChangesAsync();
        }

        return newPerson;
    }
}