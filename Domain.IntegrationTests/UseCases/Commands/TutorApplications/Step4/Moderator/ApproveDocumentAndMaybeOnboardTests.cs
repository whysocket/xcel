using Application.UseCases.Commands.TutorApplications.Step4.Moderator;
using Domain.Entities;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplications.Step4.Moderator;

public class ApproveDocumentAndMaybeOnboardTests : BaseTest
{
    [Fact]
    public async Task Handle_ApprovesDocument_AndSendsApprovalEmail()
    {
        var applicant = new Person { FirstName = "Liam", LastName = "Brown", EmailAddress = "liam@example.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            CurrentStep = TutorApplication.OnboardingStep.DocumentsRequested
        };

        var document = new TutorDocument
        {
            DocumentType = TutorDocument.TutorDocumentType.Id,
            DocumentPath = "path/id.pdf",
            Status = TutorDocument.TutorDocumentStatus.Pending,
            TutorApplication = application
        };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorDocumentsRepository.AddAsync(document);
        await TutorDocumentsRepository.SaveChangesAsync();

        var command = new ApproveDocumentAndMaybeOnboard.Command(document.Id);
        var result = await Sender.Send(command);

        Assert.True(result.IsSuccess);

        var updatedDoc = await TutorDocumentsRepository.GetByIdAsync(document.Id);
        Assert.NotNull(updatedDoc);
        Assert.Equal(TutorDocument.TutorDocumentStatus.Approved, updatedDoc.Status);

        var sentEmail = InMemoryEmailService.GetSentEmail<TutorDocumentApprovedEmail>();
        Assert.Equal(applicant.EmailAddress, sentEmail.Payload.To.First());
        Assert.Equal(document.DocumentType.ToString(), sentEmail.Payload.Data.DocumentType);
    }

    [Fact]
    public async Task Handle_ApprovesFinalDocument_AndOnboardsTutor()
    {
        var applicant = new Person { FirstName = "Noah", LastName = "Sky", EmailAddress = "noah@example.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            CurrentStep = TutorApplication.OnboardingStep.DocumentsRequested
        };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(application);

        // Add already-approved CV and ID
        var approvedDocs = new[]
        {
            TutorDocument.TutorDocumentType.Cv,
            TutorDocument.TutorDocumentType.Id
        };

        foreach (var type in approvedDocs)
        {
            await TutorDocumentsRepository.AddAsync(new TutorDocument
            {
                TutorApplication = application,
                DocumentType = type,
                DocumentPath = $"path/{type}.pdf",
                Status = TutorDocument.TutorDocumentStatus.Approved
            });
        }

        // Add pending DBS
        var finalDoc = new TutorDocument
        {
            TutorApplication = application,
            DocumentType = TutorDocument.TutorDocumentType.Dbs,
            DocumentPath = "path/dbs.pdf",
            Status = TutorDocument.TutorDocumentStatus.Pending
        };

        await TutorDocumentsRepository.AddAsync(finalDoc);
        await TutorDocumentsRepository.SaveChangesAsync();

        var command = new ApproveDocumentAndMaybeOnboard.Command(finalDoc.Id);
        var result = await Sender.Send(command);

        Assert.True(result.IsSuccess);

        var updatedApplication = await TutorApplicationsRepository.GetByIdAsync(application.Id);
        Assert.NotNull(updatedApplication);
        Assert.Equal(TutorApplication.OnboardingStep.Onboarded, updatedApplication.CurrentStep);

        var createdProfile = await TutorProfilesesRepository.GetByAsync(p => p.PersonId == applicant.Id);
        Assert.NotNull(createdProfile);
        Assert.Equal(TutorProfile.TutorProfileStatus.PendingConfiguration, createdProfile.Status);

        var onboardedEmail = InMemoryEmailService.GetSentEmail<TutorOnboardedEmail>();
        Assert.Equal(applicant.EmailAddress, onboardedEmail.Payload.To.First());
    }
}