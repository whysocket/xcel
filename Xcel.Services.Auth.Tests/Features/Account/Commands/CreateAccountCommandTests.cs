using Xcel.Services.Auth.Features.Account.Commands.Implementations;

namespace Xcel.Services.Auth.Tests.Features.Account.Commands;

public class CreateAccountCommandTests : AuthBaseTest
{
    [Fact]
    public async Task ExecuteAsync_WhenPersonIsNew_ShouldCreateAndSendWelcomeEmail()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            EmailAddress = $"john{Guid.NewGuid()}@test.com"
        };

        // Act
        var result = await CreateAccountCommand.ExecuteAsync(person);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(person.EmailAddress, result.Value.EmailAddress);

        var storedPerson = await PersonsRepository.GetByIdAsync(person.Id);
        Assert.NotNull(storedPerson);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPersonEmailAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var existingPerson = await CreatePersonAsync();

        var duplicate = new Person
        {
            FirstName = "Jane",
            LastName = "Smith",
            EmailAddress = existingPerson.EmailAddress
        };

        // Act
        var result = await CreateAccountCommand.ExecuteAsync(duplicate);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(CreateAccountCommandErrors.EmailAlreadyExists(existingPerson.EmailAddress), error);
    }
}