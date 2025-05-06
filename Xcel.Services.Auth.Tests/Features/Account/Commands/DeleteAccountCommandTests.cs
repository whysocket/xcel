using Xcel.Services.Auth.Features.Account.Commands.Implementations;

namespace Xcel.Services.Auth.Tests.Features.Account.Commands;

public class DeleteAccountCommandTests : AuthBaseTest
{
    [Fact]
    public async Task ExecuteAsync_WhenPersonExists_ShouldSoftDelete()
    {
        // Arrange
        var person = await CreateUserAsync();

        // Act
        var result = await DeleteAccountCommand.ExecuteAsync(person.Id);

        // Assert
        Assert.True(result.IsSuccess);

        var deletedPerson = await PersonsRepository.GetDeletedByIdAsync(person.Id);
        Assert.NotNull(deletedPerson);
        Assert.True(deletedPerson.IsDeleted);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPersonDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await DeleteAccountCommand.ExecuteAsync(nonExistentId);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(DeleteAccountCommandErrors.PersonNotFound(nonExistentId), error);
    }
}
