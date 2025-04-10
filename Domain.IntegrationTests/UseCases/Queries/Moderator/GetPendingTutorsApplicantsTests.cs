using Application.UseCases.Queries.Moderator;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.Moderator;

public class GetPendingTutorsApplicantsTests : BaseTest
{
    [Fact]
    public async Task Handle_ReturnsPendingTutorsWithDocuments()
    {
        // Arrange
        var pendingTutors = new List<Tutor>
        {
            new()
            {
                Person = new Person { FirstName = "John", LastName = "Doe", EmailAddress = "john.doe@example.com" },
                Status = Tutor.TutorStatus.Pending,
                TutorDocuments =
                [
                    new()
                    {
                        DocumentPath = "/path/to/cv1.pdf", DocumentType = TutorDocument.TutorDocumentType.Cv,
                        Status = TutorDocument.TutorDocumentStatus.Pending
                    }
                ]
            },
            new()
            {
                Person = new Person { FirstName = "Jane", LastName = "Smith", EmailAddress = "jane.smith@example.com" },
                Status = Tutor.TutorStatus.Pending,
                TutorDocuments =
                [
                    new()
                    {
                        DocumentPath = "/path/to/cv2.pdf", DocumentType = TutorDocument.TutorDocumentType.Cv,
                        Status = TutorDocument.TutorDocumentStatus.Pending
                    }
                ]
            }
        };

        var approvedTutor = new Tutor
        {
            Person = new Person { FirstName = "Test", LastName = "User", EmailAddress = "test@example.com" },
            Status = Tutor.TutorStatus.Approved,
            TutorDocuments =
            [
                new()
                {
                    DocumentPath = "/path/to/cv3.pdf", DocumentType = TutorDocument.TutorDocumentType.Cv,
                    Status = TutorDocument.TutorDocumentStatus.Pending
                },
                new()
                {
                    DocumentPath = "/path/to/id1.pdf", DocumentType = TutorDocument.TutorDocumentType.Id,
                    Status = TutorDocument.TutorDocumentStatus.Approved
                }
            ]
        };

        await TutorsRepository.AddRangeAsync([..pendingTutors, approvedTutor]);
        await TutorsRepository.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetPendingTutorsApplicants.Query());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(pendingTutors.Count, result.Value.TutorsApplications.Count());

        foreach (var pendingTutor in pendingTutors)
        {
            var tutorDto =
                result.Value.TutorsApplications.FirstOrDefault(t =>
                    t.Person.EmailAddress == pendingTutor.Person.EmailAddress);
            Assert.NotNull(tutorDto);
            Assert.Single(tutorDto.Documents);
            Assert.Equal(pendingTutor.TutorDocuments.First().DocumentPath, tutorDto.Documents.First().Path);
        }
    }

    [Fact]
    public async Task Handle_ReturnsEmptyListWhenNoPendingTutorsExist()
    {
        // Arrange
        var approvedTutor = new Tutor
        {
            Person = new Person { FirstName = "Test", LastName = "User", EmailAddress = "test@example.com" },
            Status = Tutor.TutorStatus.Approved,
            TutorDocuments =
            [
                new()
                {
                    DocumentPath = "/path/to/cv1.pdf", DocumentType = TutorDocument.TutorDocumentType.Cv,
                    Status = TutorDocument.TutorDocumentStatus.Approved
                }
            ]
        };

        await TutorsRepository.AddAsync(approvedTutor);
        await TutorsRepository.SaveChangesAsync();

        // Act
        var result = await Sender.Send(new GetPendingTutorsApplicants.Query());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.TutorsApplications);
    }
}