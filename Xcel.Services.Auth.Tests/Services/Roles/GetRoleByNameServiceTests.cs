using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Tests.Services.Roles;

public class GetRoleByNameServiceTests : AuthBaseTest
{
    [Fact]
    public async Task GetRoleByNameAsync_WhenRoleExists_ShouldReturnSuccessAndRole()
    {
        // Arrange
        var roleName = "Administrator";
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = roleName.ToUpperInvariant() });
        await RolesRepository.SaveChangesAsync();

        // Act
        var result = await GetRoleByNameService.GetRoleByNameAsync(roleName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(roleName.ToUpperInvariant(), result.Value.Name);
    }

    [Fact]
    public async Task GetRoleByNameAsync_WhenRoleExistsWithDifferentCase_ShouldReturnSuccessAndRole()
    {
        // Arrange
        var roleName = "editor";
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = roleName.ToUpperInvariant() });
        await RolesRepository.SaveChangesAsync();

        // Act
        var result = await GetRoleByNameService.GetRoleByNameAsync(roleName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(roleName.ToUpperInvariant(), result.Value.Name);
    }

    [Fact]
    public async Task GetRoleByNameAsync_WhenRoleDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var roleName = "NonExistentRole";

        // Act
        var result = await GetRoleByNameService.GetRoleByNameAsync(roleName);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Errors.Single().Type);
        Assert.Equal($"The role with name '{roleName.ToLowerInvariant()}' is not found.", result.Errors.Single().Message);
    }
}