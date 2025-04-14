using Xcel.Services.Auth.Implementations.Services;
using Xcel.Services.Email.Templates.WelcomeEmail;

namespace Xcel.Services.Auth.Tests.Services;

public class UserServiceTests : AuthBaseTest
{
    private Person _person = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _person = await CreatePersonAsync();
    }

    [Fact]
    public async Task CreateAccountAsync_WhenValidPerson_ShouldCreateAccountAndSendWelcomeEmail()
    {
        // Arrange
        var person = new Person
        {
            Id = Guid.NewGuid(),
            EmailAddress = "john@test.com",
            FirstName = "John",
            LastName = "Doe",
        };

        var userService = new UserService(PersonsRepository, EmailService);

        // Act
        var result = await userService.CreateAccountAsync(person);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(person.Id, result.Value.Id);

        var sentEmail = InMemoryEmailSender.GetSentEmail<WelcomeEmailData>();
        Assert.Equal(person.EmailAddress, sentEmail.Payload.To.First());
    }

    [Fact]
    public async Task CreateAccountAsync_WhenPersonWithExistingEmail_ShouldReturnFailure()
    {
        // Arrange
        var userService = new UserService(PersonsRepository, EmailService);

        // Act
        var result = await userService.CreateAccountAsync(_person);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Conflict, error.Type);
        Assert.Equal(error.Message, $"A person with the email address '{_person.EmailAddress}' already exists.");

        Assert.Throws<InvalidOperationException>(() => InMemoryEmailSender.GetSentEmail<WelcomeEmailData>());
    }

    [Fact]
    public async Task DeleteAccountAsync_WhenPersonExists_ShouldMarkPersonAsDeleted()
    {
        // Arrange
        var userService = new UserService(PersonsRepository, EmailService);

        // Act
        var result = await userService.DeleteAccountAsync(_person.Id);

        // Assert
        Assert.True(result.IsSuccess);

        var deletedPerson = await PersonsRepository.GetDeletedByIdAsync(_person.Id);
        Assert.NotNull(deletedPerson);
        Assert.True(deletedPerson.IsDeleted);
    }

    [Fact]
    public async Task DeleteAccountAsync_WhenPersonDoesNotExist_ShouldNotThrowException()
    {
        // Arrange
        var nonExistentPersonId = Guid.NewGuid();
        var userService = new UserService(PersonsRepository, EmailService);

        // Act
        var result = await userService.DeleteAccountAsync(nonExistentPersonId);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.NotFound, error.Type);

        var personFromDb = await PersonsRepository.GetByIdAsync(nonExistentPersonId);
        Assert.Null(personFromDb);
    }
}