using Xcel.Services.Auth.Models;
using Xcel.Services.Auth.Implementations.Services.PersonRoles;

namespace Xcel.Services.Auth.Tests.Services.PersonRoles;

public class AssignRoleToPersonServiceTests : AuthBaseTest
{
    [Fact]
    public async Task AssignRoleToPersonAsync_WhenPersonAndRoleExist_ShouldAssignRole()
    {
        // Arrange
        var person = await CreatePersonAsync();
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "TestRole" };
        await RolesRepository.AddAsync(role);
        await RolesRepository.SaveChangesAsync();

        // Act
        var result = await AssignRoleToPersonService.AssignRoleToPersonAsync(person.Id, role.Id);

        // Assert
        Assert.True(result.IsSuccess);

        var assignedRole = await PersonRoleRepository.GetPersonRoleAsync(person.Id, role.Id);
        Assert.NotNull(assignedRole);
        Assert.Equal(person.Id, assignedRole.PersonId);
        Assert.Equal(role.Id, assignedRole.RoleId);
    }

    [Fact]
    public async Task AssignRoleToPersonAsync_WhenRoleDoesNotExist_ShouldReturnFailureNotFound()
    {
        // Arrange
        var person = await CreatePersonAsync();
        var nonExistentRoleId = Guid.NewGuid();

        // Act
        var result = await AssignRoleToPersonService.AssignRoleToPersonAsync(person.Id, nonExistentRoleId);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(AssignRoleToPersonServiceErrors.RoleNotFound(nonExistentRoleId), resultError);
    }

    [Fact]
    public async Task AssignRoleToPersonAsync_WhenRoleAlreadyAssigned_ShouldReturnFailureConflict()
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
        var result = await AssignRoleToPersonService.AssignRoleToPersonAsync(person.Id, role.Id);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(AssignRoleToPersonServiceErrors.RoleAlreadyAssigned(), resultError); 
    }

    [Fact]
    public async Task AssignRoleToPersonAsync_WhenInvalidPersonId_ShouldReturnFailureValidation()
    {
        // Arrange
        var invalidGuid = Guid.Empty;

        // Act
        var result = await AssignRoleToPersonService.AssignRoleToPersonAsync(invalidGuid, Guid.NewGuid());

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(CommonErrors.InvalidGuid("personId"), resultError);
    }

    [Fact]
    public async Task AssignRoleToPersonAsync_WhenInvalidRoleId_ShouldReturnFailureValidation()
    {
        // Arrange
        var personId = Guid.NewGuid();

        // Act
        var result = await AssignRoleToPersonService.AssignRoleToPersonAsync(personId, Guid.Empty);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(CommonErrors.InvalidGuid("roleId"), resultError);
    }
}