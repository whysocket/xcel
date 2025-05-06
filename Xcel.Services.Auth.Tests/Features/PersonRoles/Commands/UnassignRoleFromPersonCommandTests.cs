using Xcel.Services.Auth.Features.PersonRoles.Commands.Implementations;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Tests.Features.PersonRoles.Commands;

public class UnassignRoleFromPersonCommandTests : AuthBaseTest
{
    [Fact]
    public async Task ExecuteAsync_WhenAssignmentExists_ShouldUnassignRole()
    {
        // Arrange
        var person = await CreateUserAsync();
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "TestRole" };
        await RolesRepository.AddAsync(role);
        await RolesRepository.SaveChangesAsync();

        var existingAssignment = new PersonRoleEntity { PersonId = person.Id, RoleId = role.Id };
        await PersonRoleRepository.AddAsync(existingAssignment);
        await PersonRoleRepository.SaveChangesAsync();

        // Act
        var result = await UnassignRoleFromPersonCommand.ExecuteAsync(person.Id, role.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var unassignedRole = await PersonRoleRepository.GetPersonRoleAsync(person.Id, role.Id);
        Assert.Null(unassignedRole);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAssignmentDoesNotExist_ShouldReturnFailureNotFound()
    {
        // Arrange
        var person = await CreateUserAsync();
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "TestRole" };
        await RolesRepository.AddAsync(role);
        await RolesRepository.SaveChangesAsync();

        // Act
        var result = await UnassignRoleFromPersonCommand.ExecuteAsync(person.Id, role.Id);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(UnassignRoleFromPersonServiceErrors.RoleAssignmentNotFound(), resultError);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInvalidPersonId_ShouldReturnFailureValidation()
    {
        // Arrange
        var invalidGuid = Guid.Empty;

        // Act
        var result = await UnassignRoleFromPersonCommand.ExecuteAsync(invalidGuid, Guid.NewGuid());

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(CommonErrors.InvalidGuid("personId"), resultError);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInvalidRoleId_ShouldReturnFailureValidation()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var invalidGuid = Guid.Empty;

        // Act
        var result = await UnassignRoleFromPersonCommand.ExecuteAsync(personId, invalidGuid);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(CommonErrors.InvalidGuid("roleId"), resultError);
    }
}
