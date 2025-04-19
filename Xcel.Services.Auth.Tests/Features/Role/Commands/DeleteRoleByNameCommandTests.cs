using Xcel.Services.Auth.Features.Roles.Commands.Implementations;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Tests.Features.Role.Commands;

public class DeleteRoleByNameCommandTests : AuthBaseTest
{
    [Fact]
    public async Task DeleteRoleByNameAsync_WhenRoleExists_ShouldDeleteRole()
    {
        // Arrange
        var roleName = "ToDelete";
        var roleToDelete = new RoleEntity { Id = Guid.NewGuid(), Name = roleName.ToUpperInvariant() };
        await RolesRepository.AddAsync(roleToDelete);
        await RolesRepository.SaveChangesAsync();

        // Act
        var result = await DeleteRoleByNameCommand.ExecuteAsync(roleName);

        // Assert
        Assert.True(result.IsSuccess);

        var retrievedRole = await RolesRepository.GetByIdAsync(roleToDelete.Id);
        Assert.Null(retrievedRole);
    }

    [Fact]
    public async Task DeleteRoleByNameAsync_WhenRoleExistsWithDifferentCase_ShouldDeleteRole()
    {
        // Arrange
        var roleName = "todelete";
        var roleToDelete = new RoleEntity { Id = Guid.NewGuid(), Name = roleName.ToUpperInvariant() };
        await RolesRepository.AddAsync(roleToDelete);
        await RolesRepository.SaveChangesAsync();

        // Act
        var result = await DeleteRoleByNameCommand.ExecuteAsync(roleName);

        // Assert
        Assert.True(result.IsSuccess);
        var retrievedRole = await RolesRepository.GetByIdAsync(roleToDelete.Id);
        Assert.Null(retrievedRole);
    }

    [Fact]
    public async Task DeleteRoleByNameAsync_WhenRoleDoesNotExist_ShouldReturnFailureNotFound()
    {
        // Arrange
        var nonExistentRoleName = "NonExistent";

        // Act
        var result = await DeleteRoleByNameCommand.ExecuteAsync(nonExistentRoleName);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(DeleteRoleByNameServiceErrors.RoleNotFound(nonExistentRoleName.ToLowerInvariant()), resultError); 
    }
}