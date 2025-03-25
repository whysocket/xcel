using Xcel.Services.Auth.Implementations.Services;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Tests.Services;

public class RoleServiceTests : BaseTest
{
    private IRoleService _roleService = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _roleService = new RoleService(RolesRepository);
    }

    [Fact]
    public async Task CreateRoleAsync_WhenValidRoleName_ShouldCreateRole()
    {
        // Arrange
        var roleName = "Admin";

        // Act
        var result = await _roleService.CreateRoleAsync(roleName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(roleName, result.Value.Name);

        var retrievedRole = await RolesRepository.GetRoleByNameIgnoreCaseAsync(roleName.ToLower());
        Assert.NotNull(retrievedRole);
        Assert.Equal(roleName, retrievedRole.Name);
    }

    [Fact]
    public async Task CreateRoleAsync_WhenRoleNameAlreadyExists_ShouldReturnFailure()
    {
        // Arrange
        var roleName = "Admin";
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = roleName });
        await RolesRepository.SaveChangesAsync();

        // Act
        var result = await _roleService.CreateRoleAsync(roleName);

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
        var result = await _roleService.CreateRoleAsync(roleName);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Errors.Single().Type);
        Assert.Equal("The role name is required", result.Errors.Single().Message);
    }

    [Fact]
    public async Task GetAllRolesAsync_ShouldReturnAllRoles()
    {
        // Arrange
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = "Admin" });
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = "User" });
        await RolesRepository.SaveChangesAsync();

        // Act
        var result = await _roleService.GetAllRolesAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task UpdateRoleAsync_WhenRoleExists_ShouldUpdateRoleName()
    {
        // Arrange
        var existingRole = new RoleEntity { Id = Guid.NewGuid(), Name = "Admin" };
        await RolesRepository.AddAsync(existingRole);
        await RolesRepository.SaveChangesAsync();

        var newRoleName = "SuperAdmin";

        // Act
        var result = await _roleService.UpdateRoleAsync(existingRole.Id, newRoleName);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedRole = await RolesRepository.GetByIdAsync(existingRole.Id);
        Assert.NotNull(updatedRole);
        Assert.Equal(newRoleName, updatedRole.Name);
    }

    [Fact]
    public async Task UpdateRoleAsync_WhenRoleDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var newRoleName = "SuperAdmin";

        // Act
        var result = await _roleService.UpdateRoleAsync(roleId, newRoleName);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Errors.Single().Type);
        Assert.Equal($"The role with id '{roleId}' is not found.", result.Errors.Single().Message);
    }

    [Fact]
    public async Task UpdateRoleAsync_WhenNewRoleNameIsNullOrWhiteSpace_ShouldReturnFailure()
    {
        // Arrange
        var existingRole = new RoleEntity { Id = Guid.NewGuid(), Name = "Admin" };
        await RolesRepository.AddAsync(existingRole);
        await RolesRepository.SaveChangesAsync();

        var newRoleName = "";

        // Act
        var result = await _roleService.UpdateRoleAsync(existingRole.Id, newRoleName);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Errors.Single().Type);
        Assert.Equal("The new role name is required", result.Errors.Single().Message);
    }

    [Fact]
    public async Task UpdateRoleAsync_WhenNewRoleNameAlreadyExists_ShouldReturnFailure()
    {
        // Arrange
        var existingRole = new RoleEntity { Id = Guid.NewGuid(), Name = "Admin" };
        var existingRoleToUpdate = new RoleEntity { Id = Guid.NewGuid(), Name = "Moderator" };

        await RolesRepository.AddRangeAsync([existingRole, existingRoleToUpdate]);
        await RolesRepository.SaveChangesAsync();

        var newRoleName = "aDmIn";

        // Act
        var result = await _roleService.UpdateRoleAsync(existingRoleToUpdate.Id, newRoleName);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Errors.Single().Type);
        Assert.Equal($"The role '{newRoleName}' already exists.", result.Errors.Single().Message);
    }

    [Fact]
    public async Task DeleteRoleByNameAsync_WhenRoleExists_ShouldDeleteRole()
    {
        // Arrange
        var roleName = "Admin";
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = roleName });
        await RolesRepository.SaveChangesAsync();

        // Act
        var result = await _roleService.DeleteRoleByNameAsync(roleName);

        // Assert
        Assert.True(result.IsSuccess);

        var deletedRole = await RolesRepository.GetRoleByNameIgnoreCaseAsync(roleName.ToLower());
        Assert.Null(deletedRole);
    }

    [Fact]
    public async Task DeleteRoleByNameAsync_WhenRoleDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var roleName = "Admin";

        // Act
        var result = await _roleService.DeleteRoleByNameAsync(roleName);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Errors.Single().Type);
        Assert.Equal($"The role '{roleName}' is not found.", result.Errors.Single().Message);
    }
}