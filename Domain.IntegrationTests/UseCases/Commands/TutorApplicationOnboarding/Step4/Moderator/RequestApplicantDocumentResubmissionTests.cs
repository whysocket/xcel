using Application.UseCases.Commands.TutorApplicationOnboarding.Step4.Moderator;
using Domain.Entities;
using Domain.Exceptions;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplicationOnboarding.Step4.Moderator;

public class RequestApplicantDocumentResubmissionTests : BaseTest
{
    [Fact]
    public async Task Handle_RequestsResubmissionAndSendsEmailSuccessfully()
    {
        // Arrange
        var applicant = new Person { FirstName = "Liam", LastName = "Frost", EmailAddress = "liam@example.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            CurrentStep = TutorApplication.OnboardingStep.DocumentsRequested,
            Documents =
            [
                new TutorDocument
                {
                    DocumentType = TutorDocument.TutorDocumentType.Dbs,
                    DocumentPath = "some-path.pdf",
                    Status = TutorDocument.TutorDocumentStatus.Pending,
                    Version = 1
                }
            ]
        };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var document = application.Documents.First();
        var command = new RequestApplicantDocumentResubmission.Command(document.Id, "Document is blurry");

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedDocument = await TutorDocumentsRepository.GetByIdAsync(document.Id);
        Assert.NotNull(updatedDocument);
        Assert.Equal(TutorDocument.TutorDocumentStatus.ResubmissionNeeded, updatedDocument.Status);
        Assert.Equal("Document is blurry", updatedDocument.ModeratorReason);

        var sentEmail = InMemoryEmailService.GetSentEmail<TutorDocumentResubmissionRequestedEmail>();
        Assert.Equal(applicant.EmailAddress, sentEmail.Payload.To.First());
        Assert.Equal("Dbs", sentEmail.Payload.Data.DocumentType);
        Assert.Equal("Document is blurry", sentEmail.Payload.Data.RejectionReason);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenDocumentNotFound()
    {
        // Arrange
        var command = new RequestApplicantDocumentResubmission.Command(Guid.NewGuid(), "Reason");

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(RequestApplicantDocumentResubmission.Errors.Handler.NotFound.Message, error.Message);
    }

    [Fact]
    public async Task Handle_ReturnsValidationError_WhenReasonIsEmpty()
    {
        // Arrange
        var command = new RequestApplicantDocumentResubmission.Command(Guid.NewGuid(), "");

        // Act
        var ex = await Assert.ThrowsAsync<DomainValidationException>(() => Sender.Send(command));

        // Assert
        var result = ex.ToResult();
        var error = Assert.Single(result.Errors);
        Assert.Equal(RequestApplicantDocumentResubmission.Errors.Command.RejectReasonIsRequired, error.Message);
    }
}
