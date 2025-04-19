using Xcel.Services.Auth.Features.Roles.Queries.Implementations;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Tests.Features.Role.Queries;

public class GetRoleByNameQueryTests : AuthBaseTest
{
    [Fact]
    public async Task ExecuteAsync_WhenRoleExists_ShouldReturnSuccessAndRole()
    {
        // Arrange
        var roleName = "Administrator";
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = roleName.ToUpperInvariant() });
        await RolesRepository.SaveChangesAsync();

        // Act
        var result = await GetRoleByNameQuery.ExecuteAsync(roleName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(roleName.ToUpperInvariant(), (string?)result.Value.Name);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleExistsWithDifferentCase_ShouldReturnSuccessAndRole()
    {
        // Arrange
        var roleName = "editor";
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = roleName.ToUpperInvariant() });
        await RolesRepository.SaveChangesAsync();

        // Act
        var result = await GetRoleByNameQuery.ExecuteAsync(roleName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(roleName.ToUpperInvariant(), (string?)result.Value.Name);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var roleName = "NonExistentRole";

        // Act
        var result = await GetRoleByNameQuery.ExecuteAsync(roleName);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(GetRoleByNameServiceErrors.RoleNotFound(roleName.ToLowerInvariant()), resultError);
    }
}