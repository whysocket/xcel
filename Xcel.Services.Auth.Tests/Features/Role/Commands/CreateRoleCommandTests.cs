using Xcel.Services.Auth.Features.Roles.Commands.Implementations;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Tests.Features.Role.Commands;

public class CreateRoleCommandTests : AuthBaseTest
{
    [Fact]
    public async Task CreateRoleAsync_WhenValidRoleName_ShouldCreateRole()
    {
        // Arrange
        var roleName = "admin";

        // Act
        var result = await CreateRoleCommand.ExecuteAsync(roleName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(roleName, (string?)result.Value.Name);

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
        var result = await CreateRoleCommand.ExecuteAsync(roleName);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(CreateRoleServiceErrors.RoleAlreadyExists(roleName), resultError);
    }

    [Fact]
    public async Task CreateRoleAsync_WhenRoleNameIsNullOrWhiteSpace_ShouldReturnFailure()
    {
        // Arrange
        var roleName = "";

        // Act
        var result = await CreateRoleCommand.ExecuteAsync(roleName);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(CreateRoleServiceErrors.RoleNameRequired(), resultError);
    }
}