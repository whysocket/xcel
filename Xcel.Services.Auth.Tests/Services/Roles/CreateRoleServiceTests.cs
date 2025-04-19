using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Tests.Services.Roles;

public class CreateRoleServiceTests : AuthBaseTest
{
    [Fact]
    public async Task CreateRoleAsync_WhenValidRoleName_ShouldCreateRole()
    {
        // Arrange
        var roleName = "Admin";

        // Act
        var result = await CreateRoleService.CreateRoleAsync(roleName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(roleName, result.Value.Name);

        var retrievedRole = await RolesRepository.GetByNameInsensitiveAsync(roleName);
        Assert.NotNull(retrievedRole);
        Assert.Equal(roleName, retrievedRole.Name);
    }

    [Fact]
    public async Task CreateRoleAsync_WhenRoleNameAlreadyExists_ShouldReturnFailure()
    {
        // Arrange
        var roleName = "admin";
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = roleName });
        await RolesRepository.SaveChangesAsync();

        // Act
        var result = await CreateRoleService.CreateRoleAsync(roleName);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Errors.Single().Type);
        Assert.Equal($"The role '{roleName}' already exists.", result.Errors.Single().Message);
    }

    [Fact]
    public async Task CreateRoleAsync_WhenRoleNameIsNullOrWhiteSpace_ShouldReturnFailure()
    {
        // Arrange
        var roleName = "";

        // Act
        var result = await CreateRoleService.CreateRoleAsync(roleName);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Errors.Single().Type);
        Assert.Equal("The role name is required", result.Errors.Single().Message);
    }
}