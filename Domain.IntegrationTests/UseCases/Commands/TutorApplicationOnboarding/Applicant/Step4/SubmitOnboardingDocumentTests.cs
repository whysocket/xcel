using System.Net.Mime;
using Application.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step4;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Payloads;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step4;

public class SubmitOnboardingDocumentTests : BaseTest
{
    [Fact]
    public async Task Handle_UploadsIdDocument_Successfully()
    {
        // Arrange
        var applicant = new Person { FirstName = "Ella", LastName = "Grey", EmailAddress = "ella@example.com" };
        var reviewer = new Person { FirstName = "Ismael", LastName = "Sun", EmailAddress = "reviewerisma@example.com" };

        var tutorApplication = new TutorApplication
        {
            Applicant = applicant,
            CurrentStep = TutorApplication.OnboardingStep.DocumentsAnalysis,
            Interview = new()
            {
                Reviewer = reviewer
            }
        };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var documentPayload = new DocumentPayload("id-passport.pdf", MediaTypeNames.Application.Pdf, [0x25, 0x50, 0x44, 0x46]);
        var command = new SubmitOnboardingDocument.Command(tutorApplication.Id, TutorDocument.TutorDocumentType.Id, documentPayload);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedTutorApplication = await TutorApplicationsRepository.GetByIdAsync(tutorApplication.Id);
        Assert.NotNull(updatedTutorApplication);
        var uploadedDoc = updatedTutorApplication.Documents.Single(d => d.DocumentType == TutorDocument.TutorDocumentType.Id);

        Assert.Equal(1, uploadedDoc.Version);
        Assert.Equal(TutorDocument.TutorDocumentStatus.Pending, uploadedDoc.Status);
        Assert.Equal("id-passport.pdf", Path.GetFileName(uploadedDoc.DocumentPath)[^"id-passport.pdf".Length..]);
        
        var sentEmail = InMemoryEmailService.GetSentEmail<TutorDocumentSubmittedToReviewerEmail>();
        Assert.Equal(reviewer.EmailAddress, sentEmail.Payload.To.First());
    }
    
    [Fact]
    public async Task Handle_AssignsIncrementedVersion_WhenSameDocumentTypeIsResubmitted()
    {
        // Arrange
        var applicant = new Person { FirstName = "Maya", LastName = "Bright", EmailAddress = "maya@example.com" };
        var reviewer = new Person { FirstName = "Ismael", LastName = "Sun", EmailAddress = "reviewerisma@example.com" };

        var tutorApplication = new TutorApplication
        {
            Applicant = applicant,
            CurrentStep = TutorApplication.OnboardingStep.DocumentsAnalysis,
            Interview = new()
            {
                Reviewer = reviewer
            }
        };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var firstDocumentPayload = new DocumentPayload("id-v1.pdf", MediaTypeNames.Application.Pdf, [0x25, 0x50, 0x44, 0x46]);
        var secondDocumentPayload = new DocumentPayload("id-v2.pdf", MediaTypeNames.Application.Pdf, [0x25, 0x50, 0x44, 0x46]);

        var firstCommand = new SubmitOnboardingDocument.Command(tutorApplication.Id, TutorDocument.TutorDocumentType.Id, firstDocumentPayload);
        var secondCommand = new SubmitOnboardingDocument.Command(tutorApplication.Id, TutorDocument.TutorDocumentType.Id, secondDocumentPayload);

        // Act
        var firstResult = await Sender.Send(firstCommand);
        var secondResult = await Sender.Send(secondCommand);

        // Assert
        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);

        var updatedTutorApplication = await TutorApplicationsRepository.GetByIdAsync(tutorApplication.Id);
        Assert.NotNull(updatedTutorApplication);
        var idDocuments = updatedTutorApplication.Documents
            .Where(d => d.DocumentType == TutorDocument.TutorDocumentType.Id)
            .OrderBy(d => d.Version)
            .ToList();

        Assert.Equal(2, idDocuments.Count);

        var firstDocument = idDocuments[0];
        var secondDocument = idDocuments[1];

        Assert.Equal(1, firstDocument.Version);
        Assert.Equal(2, secondDocument.Version);

        Assert.EndsWith("id-v1.pdf", Path.GetFileName(firstDocument.DocumentPath));
        Assert.EndsWith("id-v2.pdf", Path.GetFileName(secondDocument.DocumentPath));

        var sentEmails = InMemoryEmailService.GetSentEmails<TutorDocumentSubmittedToReviewerEmail>();
        Assert.Equal(2, sentEmails.Count);
        Assert.All(sentEmails, sentEmail => Assert.Equal(reviewer.EmailAddress, sentEmail.Payload.To.First()));
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenApplicationNotInCorrectStep()
    {
        // Arrange
        var tutorApplication = new TutorApplication
        {
            Applicant = new Person { FirstName = "Max", LastName = "Stone", EmailAddress = "max@example.com" },
            CurrentStep = TutorApplication.OnboardingStep.InterviewBooking
        };

        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var documentPayload = new DocumentPayload("id", MediaTypeNames.Application.Pdf, [0x25, 0x50, 0x44, 0x46]);
        var command = new SubmitOnboardingDocument.Command(tutorApplication.Id, TutorDocument.TutorDocumentType.Id, documentPayload);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Tutor application is not ready to receive documents.", error.Message);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenUploadFails()
    {
        // Arrange
        var application = new TutorApplication
        {
            Applicant = new Person { FirstName = "Anna", LastName = "Nova", EmailAddress = "anna@example.com" },
            CurrentStep = TutorApplication.OnboardingStep.DocumentsAnalysis
        };

        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new SubmitOnboardingDocument.Command(
            application.Id,
            TutorDocument.TutorDocumentType.Id,
            new DocumentPayload("fail-trigger", MediaTypeNames.Application.Zip, [0x25, 0x50, 0x44, 0x46]));

        // Act
        var resultException = await Assert.ThrowsAsync<DomainValidationException>(() => Sender.Send(command));

        // Assert
        Assert.NotNull(resultException);

        var exceptionResult = resultException.ToResult();
        var error = Assert.Single(exceptionResult.Errors);
        Assert.Equal(DocumentPayloadValidator.Errors.InvalidPdfContentType, error.Message);
    }
}