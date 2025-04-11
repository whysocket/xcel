using Application.UseCases.Queries.Moderator;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.TutorApplications;

public class GetPendingTutorsApplicantsTests : BaseTest
{
    [Fact]
    public async Task Handle_ReturnsPendingTutorApplicationsWithDocuments()
    {
        // Arrange
        var pendingTutorApplications = new List<TutorApplication>
        {
            new()
            {
                Person = new Person { FirstName = "John", LastName = "Doe", EmailAddress = "john.doe@example.com" },
                CurrentStep = TutorApplication.OnboardingStep.CvUnderReview,
                Documents = 
                [
                    new()
                    {
                        DocumentPath = "/path/to/cv1.pdf",
                        DocumentType = TutorDocument.TutorDocumentType.Cv,
                        Status = TutorDocument.TutorDocumentStatus.Pending
                    }
                ]
            },
            new()
            {
                Person = new Person { FirstName = "Jane", LastName = "Smith", EmailAddress = "jane.smith@example.com" },
                CurrentStep = TutorApplication.OnboardingStep.CvUnderReview,
                Documents =
                [
                    new()
                    {
                        DocumentPath = "/path/to/cv2.pdf",
                        DocumentType = TutorDocument.TutorDocumentType.Cv,
                        Status = TutorDocument.TutorDocumentStatus.Pending
                    }
                ]
            }
        };

        var approvedTutorApplication = new TutorApplication
        {
            Person = new Person { FirstName = "Test", LastName = "User", EmailAddress = "test@example.com" },
            CurrentStep = TutorApplication.OnboardingStep.Onboarded,
            Documents = 
            [
                new()
                {
                    DocumentPath = "/path/to/cv3.pdf", 
                    DocumentType = TutorDocument.TutorDocumentType.Cv,
                    Status = TutorDocument.TutorDocumentStatus.Approved
                },
                new()
                {
                    DocumentPath = "/path/to/id1.pdf", 
                    DocumentType = TutorDocument.TutorDocumentType.Id,
                    Status = TutorDocument.TutorDocumentStatus.Pending
                }
            ]
        };

        await TutorApplicationsRepository.AddRangeAsync([..pendingTutorApplications, approvedTutorApplication]);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetPendingTutorsApplicants.Query());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(pendingTutorApplications.Count, result.Value.TutorsApplications.Count());

        foreach (var pendingTutor in pendingTutorApplications)
        {
            var tutorDto =
                result.Value.TutorsApplications.FirstOrDefault(t =>
                    t.Person.EmailAddress == pendingTutor.Person.EmailAddress);
            Assert.NotNull(tutorDto);
            Assert.Single(tutorDto.Documents);
            Assert.Equal(pendingTutor.Documents.First().DocumentPath, tutorDto.Documents.First().Path);
        }
    }

    [Fact]
    public async Task Handle_ReturnsEmptyListWhenNoPendingTutorApplicationsExist()
    {
        // Arrange
        var approvedTutorApplication = new TutorApplication
        {
            Person = new Person { FirstName = "Test", LastName = "User", EmailAddress = "test@example.com" },
            CurrentStep = TutorApplication.OnboardingStep.Onboarded,
            Documents =
            [
                new()
                {
                    DocumentPath = "/path/to/cv1.pdf", 
                    DocumentType = TutorDocument.TutorDocumentType.Cv,
                    Status = TutorDocument.TutorDocumentStatus.Approved
                }
            ]
        };

        await TutorApplicationsRepository.AddAsync(approvedTutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetPendingTutorsApplicants.Query());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.TutorsApplications);
    }
}