using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Tests.Services.Roles;

public class DeleteRoleByNameServiceTests : AuthBaseTest
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
        var result = await DeleteRoleByNameService.DeleteRoleByNameAsync(roleName);

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
        var result = await DeleteRoleByNameService.DeleteRoleByNameAsync(roleName);

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
        var result = await DeleteRoleByNameService.DeleteRoleByNameAsync(nonExistentRoleName);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Errors.Single().Type);
        Assert.Equal($"The role '{nonExistentRoleName.ToLowerInvariant()}' is not found.", result.Errors.Single().Message);
    }
}