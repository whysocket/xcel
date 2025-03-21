using System.Net.Mime;
using Application.UseCases.Commands;
using Domain.Payloads;
using Xcel.Services.Email.Templates.OtpEmail;
using Xcel.Services.Email.Templates.WelcomeEmail;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands;

public class TutorInitialApplicationSubmissionTests : BaseTest
{
    [Fact]
    public async Task TutorInitialApplicationSubmission_ValidRequest_ShouldCreateTutor()
    {
        // Arrange
        var command = new TutorInitialApplicationSubmission.Command(
            "John",
            "Doe",
            "john.doe@example.com",
            new DocumentPayload("test.pdf", MediaTypeNames.Application.Pdf, [0x25, 0x50, 0x44, 0x46]));

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        var tutor = await TutorsRepository.GetTutorWithDocuments(result.Value);
        Assert.NotNull(tutor);
        Assert.Equal(tutor.Id, result.Value);

        var cvDocument = Assert.Single(tutor.TutorDocuments);
        var uploadedFileContent = InMemoryFileService.GetFile(cvDocument.DocumentPath);

        Assert.NotNull(uploadedFileContent);
        Assert.Equal(command.CurriculumVitae.Content, uploadedFileContent);

        InMemoryEmailSender.GetSentEmail<WelcomeEmailData>();
    }
}