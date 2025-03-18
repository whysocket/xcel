using Application.UseCases.Commands.Admin;
using Domain.Entities;
using Xcel.Services.Email.Templates.TutorApprovalEmail;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.Admin;

public class ApproveTutorApplicantTests : BaseTest
{
    [Fact]
    public async Task Handle_ApprovesPendingTutorAndSendsEmail()
    {
        // Arrange
        var person = new Person { FirstName = "John", LastName = "Doe", EmailAddress = "john.doe@example.com" };
        var tutor = new Tutor
        {
            Person = person,
            Status = Tutor.TutorStatus.Pending,
            CurrentStep = Tutor.OnboardingStep.DocumentsUploaded
        };

        await TutorsRepository.AddAsync(tutor);
        await TutorsRepository.SaveChangesAsync();

        var command = new ApproveTutorApplicant.Command(tutor.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedTutor = await TutorsRepository.GetByIdAsync(tutor.Id);
        Assert.Equal(Tutor.TutorStatus.Approved, updatedTutor.Status);
        Assert.Equal(Tutor.OnboardingStep.ProfileValidated, updatedTutor.CurrentStep);

        var sentEmail = InMemoryEmailSender.GetSentEmail<TutorApprovalEmailData>();

        Assert.Equal("Your Tutor Application Status", sentEmail.Payload.Subject);
        Assert.Equal(person.EmailAddress, sentEmail.Payload.To);
        Assert.Equal(person.FirstName, sentEmail.Payload.Data.FirstName);
        Assert.Equal(person.LastName, sentEmail.Payload.Data.LastName);
    }

    [Fact]
    public async Task Handle_ReturnsFailureWhenTutorNotFound()
    {
        // Arrange
        var command = new ApproveTutorApplicant.Command(Guid.NewGuid());

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal($"Tutor with ID {command.TutorId} not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_ReturnsFailureWhenTutorIsNotPending()
    {
        // Arrange
        var person = new Person { FirstName = "John", LastName = "Doe", EmailAddress = "john.doe@example.com" };
        var tutor = new Tutor
        {
            Person = person,
            Status = Tutor.TutorStatus.Approved,
            CurrentStep = Tutor.OnboardingStep.ProfileValidated
        };

        await TutorsRepository.AddAsync(tutor);
        await TutorsRepository.SaveChangesAsync();

        var command = new ApproveTutorApplicant.Command(tutor.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal($"Tutor with ID {command.TutorId} is not in a pending state.", result.ErrorMessage);
    }
}