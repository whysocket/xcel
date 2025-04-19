using Xcel.Services.Auth.Models;
using Xcel.Services.Auth.Implementations.Services.Roles;

namespace Xcel.Services.Auth.Tests.Services.Roles;

public class UpdateRoleServiceTests : AuthBaseTest
{
    [Fact]
    public async Task UpdateRoleAsync_WhenRoleExistsAndNewNameIsValid_ShouldUpdateRole()
    {
        // Arrange
        var existingRole = new RoleEntity { Id = Guid.NewGuid(), Name = "OldAdmin" };
        await RolesRepository.AddAsync(existingRole);
        await RolesRepository.SaveChangesAsync();

        var newRoleName = "NewAdmin";

        // Act
        var result = await UpdateRoleService.UpdateRoleAsync(existingRole.Id, newRoleName);

        // Assert
        Assert.True(result.IsSuccess);
        var updatedRole = await RolesRepository.GetByIdAsync(existingRole.Id);
        Assert.NotNull(updatedRole);
        Assert.Equal(newRoleName, updatedRole.Name);
    }

    [Fact]
    public async Task UpdateRoleAsync_WhenRoleDoesNotExist_ShouldReturnFailureNotFound()
    {
        // Arrange
        var nonExistentRoleId = Guid.NewGuid();
        var newRoleName = "Admin";

        // Act
        var result = await UpdateRoleService.UpdateRoleAsync(nonExistentRoleId, newRoleName);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(UpdateRoleServiceErrors.RoleNotFound(nonExistentRoleId), resultError);
    }

    [Fact]
    public async Task UpdateRoleAsync_WhenNewRoleNameAlreadyExists_ShouldReturnFailureConflict()
    {
        // Arrange
        var existingRole1 = new RoleEntity { Id = Guid.NewGuid(), Name = "Admin" };
        var existingRole2 = new RoleEntity { Id = Guid.NewGuid(), Name = "Editor" };
        await RolesRepository.AddAsync(existingRole1);
        await RolesRepository.AddAsync(existingRole2);
        await RolesRepository.SaveChangesAsync();

        var newRoleName = "editor";

        // Act
        var result = await UpdateRoleService.UpdateRoleAsync(existingRole1.Id, newRoleName);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(UpdateRoleServiceErrors.RoleNameConflict(newRoleName), resultError);
    }

    [Fact]
    public async Task UpdateRoleAsync_WhenNewRoleNameIsNullOrWhiteSpace_ShouldReturnFailureValidation()
    {
        // Arrange
        var existingRole = new RoleEntity { Id = Guid.NewGuid(), Name = "OldAdmin" };
        await RolesRepository.AddAsync(existingRole);
        await RolesRepository.SaveChangesAsync();

        var newRoleName = "";

        // Act
        var result = await UpdateRoleService.UpdateRoleAsync(existingRole.Id, newRoleName);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(UpdateRoleServiceErrors.RoleNameRequired(), resultError);
    }

    [Fact]
    public async Task UpdateRoleAsync_WhenInvalidRoleId_ShouldReturnFailureValidation()
    {
        // Arrange
        var invalidRoleId = Guid.Empty;
        var newRoleName = "NewAdmin";

        // Act
        var result = await UpdateRoleService.UpdateRoleAsync(invalidRoleId, newRoleName);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(UpdateRoleServiceErrors.InvalidRoleId(), resultError);
    }
}