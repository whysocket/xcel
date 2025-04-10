using Application.UseCases.Commands.Moderator;
using Domain.Entities;
using Domain.Results;
using Xcel.Services.Email.Templates.TutorRejectionEmail;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.Moderator;

public class RejectTutorApplicantTests : BaseTest
{
    [Fact]
    public async Task Handle_RejectsPendingTutorAndSendsEmailAndDeleteAccount()
    {
        // Arrange
        var person = new Person { FirstName = "Jane", LastName = "Smith", EmailAddress = "jane.smith@example.com" };
        var tutor = new Tutor
        {
            Person = person,
            Status = Tutor.TutorStatus.Pending,
            CurrentStep = Tutor.OnboardingStep.DocumentsUploaded
        };

        await TutorsRepository.AddAsync(tutor);
        await TutorsRepository.SaveChangesAsync();

        var rejectionReason = "Insufficient qualifications.";
        var command = new RejectTutorApplicant.Command(tutor.Id, rejectionReason);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedTutor = await TutorsRepository.GetByIdAsync(tutor.Id);
        Assert.NotNull(updatedTutor);
        Assert.Equal(Tutor.TutorStatus.Rejected, updatedTutor.Status);
        Assert.Equal(Tutor.OnboardingStep.ApplicationDenied, updatedTutor.CurrentStep);

        var sentEmail = InMemoryEmailSender.GetSentEmail<TutorRejectionEmailData>();
        Assert.Equal("Your Tutor Application Status", sentEmail.Payload.Subject);
        Assert.Equal(person.EmailAddress, sentEmail.Payload.To);
        Assert.Equal(person.FirstName, sentEmail.Payload.Data.FirstName);
        Assert.Equal(person.LastName, sentEmail.Payload.Data.LastName);
        Assert.Equal(rejectionReason, sentEmail.Payload.Data.RejectionReason);

        Assert.Null(await PersonsRepository.GetByIdAsync(person.Id));
        Assert.NotNull(await PersonsRepository.GetDeletedByIdAsync(person.Id));
    }

    [Fact]
    public async Task Handle_ReturnsFailureWhenTutorNotFound()
    {
        // Arrange
        var command = new RejectTutorApplicant.Command(Guid.NewGuid(), "Reason");

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.Single(result.Errors);
        Assert.True(result.IsFailure);
        Assert.Equal(new Error(ErrorType.NotFound, $"Tutor with ID '{command.TutorId}' not found."), result.Errors.Single());
    }

    [Fact]
    public async Task Handle_ReturnsFailureWhenTutorIsNotPending()
    {
        // Arrange
        var person = new Person { FirstName = "Jane", LastName = "Smith", EmailAddress = "jane.smith@example.com" };
        var tutor = new Tutor
        {
            Person = person,
            Status = Tutor.TutorStatus.Approved,
            CurrentStep = Tutor.OnboardingStep.ProfileValidated
        };

        await TutorsRepository.AddAsync(tutor);
        await TutorsRepository.SaveChangesAsync();

        var command = new RejectTutorApplicant.Command(tutor.Id, "Reason");

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.Single(result.Errors);
        Assert.True(result.IsFailure);
        Assert.Equal(new Error(ErrorType.Validation, $"Tutor with ID '{command.TutorId}' is not in a pending state."), result.Errors.Single());
    }
}