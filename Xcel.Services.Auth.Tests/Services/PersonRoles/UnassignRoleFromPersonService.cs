using Xcel.Services.Auth.Models;
using Xcel.Services.Auth.Implementations.Services.PersonRoles; 

namespace Xcel.Services.Auth.Tests.Services.PersonRoles;

public class UnassignRoleFromPersonCommandTests : AuthBaseTest
{
    [Fact]
    public async Task UnassignRoleFromPersonAsync_WhenAssignmentExists_ShouldUnassignRole()
    {
        // Arrange
        var person = await CreatePersonAsync();
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "TestRole" };
        await RolesRepository.AddAsync(role);
        await RolesRepository.SaveChangesAsync();

        var existingAssignment = new PersonRoleEntity { PersonId = person.Id, RoleId = role.Id };
        await PersonRoleRepository.AddAsync(existingAssignment);
        await PersonRoleRepository.SaveChangesAsync();

        // Act
        var result = await UnassignRoleFromPersonCommand.UnassignRoleFromPersonAsync(person.Id, role.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var unassignedRole = await PersonRoleRepository.GetPersonRoleAsync(person.Id, role.Id);
        Assert.Null(unassignedRole);
    }

    [Fact]
    public async Task UnassignRoleFromPersonAsync_WhenAssignmentDoesNotExist_ShouldReturnFailureNotFound()
    {
        // Arrange
        var person = await CreatePersonAsync();
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "TestRole" };
        await RolesRepository.AddAsync(role);
        await RolesRepository.SaveChangesAsync();

        // Act
        var result = await UnassignRoleFromPersonCommand.UnassignRoleFromPersonAsync(person.Id, role.Id);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(UnassignRoleFromPersonServiceErrors.RoleAssignmentNotFound(), resultError);
    }

    [Fact]
    public async Task UnassignRoleFromPersonAsync_WhenInvalidPersonId_ShouldReturnFailureValidation()
    {
        // Arrange
        var invalidGuid = Guid.Empty;

        // Act
        var result = await UnassignRoleFromPersonCommand.UnassignRoleFromPersonAsync(invalidGuid, Guid.NewGuid());

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(CommonErrors.InvalidGuid("personId"), resultError);
    }

    [Fact]
    public async Task UnassignRoleFromPersonAsync_WhenInvalidRoleId_ShouldReturnFailureValidation()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var invalidGuid = Guid.Empty;

        // Act
        var result = await UnassignRoleFromPersonCommand.UnassignRoleFromPersonAsync(personId, invalidGuid);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(CommonErrors.InvalidGuid("roleId"), resultError);
    }
}