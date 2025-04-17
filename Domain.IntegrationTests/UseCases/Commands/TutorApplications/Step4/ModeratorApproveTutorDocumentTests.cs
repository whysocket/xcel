using Application.UseCases.Commands.TutorApplications.Step4;
using Domain.Entities;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplications.Step4;

public class ModeratorApproveTutorDocumentTests : BaseTest
{
    [Fact]
    public async Task Handle_ApprovesDocumentAndSendsEmailSuccessfully()
    {
        // Arrange
        var applicant = new Person { FirstName = "Luna", LastName = "Hope", EmailAddress = "luna@example.com" };
        var tutorApplication = new TutorApplication
        {
            Applicant = applicant,
            CurrentStep = TutorApplication.OnboardingStep.DocumentsRequested,
            Documents =
            [
                new TutorDocument
                {
                    DocumentType = TutorDocument.TutorDocumentType.Id,
                    DocumentPath = "some-path.pdf",
                    Status = TutorDocument.TutorDocumentStatus.Pending,
                    Version = 1
                }
            ]
        };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var document = tutorApplication.Documents.First();
        var command = new ModeratorApproveTutorDocument.Command(document.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedDocument = await TutorDocumentsRepository.GetByIdAsync(document.Id);
        Assert.NotNull(updatedDocument);
        Assert.Equal(TutorDocument.TutorDocumentStatus.Approved, updatedDocument.Status);
        Assert.Null(updatedDocument.ModeratorReason);

        var sentEmail = InMemoryEmailService.GetSentEmail<TutorDocumentApprovedEmail>();
        Assert.Equal(applicant.EmailAddress, sentEmail.Payload.To.First());
        Assert.Equal("Id", sentEmail.Payload.Data.DocumentType);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenDocumentNotFound()
    {
        // Arrange
        var command = new ModeratorApproveTutorDocument.Command(Guid.NewGuid());

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ModeratorApproveTutorDocument.Errors.Handler.NotFound.Message, error.Message);
    }
}
