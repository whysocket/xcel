using System.Net.Mime;
using Application.UseCases.Commands.TutorApplicationOnboarding.Step1;
using Domain.Payloads;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplicationOnboarding.Step1;

public class TutorApplicationSubmitTests : BaseTest
{
    [Fact]
    public async Task TutorInitialApplicationSubmission_ValidRequest_ShouldCreateTutor()
    {
        // Arrange
        var command = new TutorApplicationSubmit.Command(
            "John",
            "Doe",
            "john.doe@example.com",
            new DocumentPayload("test.pdf", MediaTypeNames.Application.Pdf, [0x25, 0x50, 0x44, 0x46]));

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        var tutor = await TutorApplicationsRepository.GetTutorWithDocuments(result.Value);
        Assert.NotNull(tutor);
        Assert.Equal(tutor.Id, result.Value);

        var cvDocument = Assert.Single(tutor.Documents);
        var uploadedFileContent = InMemoryFileService.GetFile(cvDocument.DocumentPath);

        Assert.NotNull(uploadedFileContent);
        Assert.Equal(command.CurriculumVitae.Content, uploadedFileContent);
        
        var person = await PersonsRepository.GetByIdAsync(tutor.ApplicantId);
        Assert.NotNull(person);

        var sentEmail = InMemoryEmailService.GetSentEmail<WelcomeEmail>();
        Assert.Equal(person.EmailAddress, sentEmail.Payload.To.First());
    }
}