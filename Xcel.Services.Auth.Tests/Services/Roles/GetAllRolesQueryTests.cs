using Domain.Interfaces.Repositories.Shared;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Tests.Services.Roles;

public class GetAllRolesQueryTests : AuthBaseTest
{
    [Fact]
    public async Task GetAllRolesAsync_WhenNoRolesExist_ShouldReturnEmptyPageResult()
    {
        // Arrange
        var pageRequest = new PageRequest(1, 10);

        // Act
        var result = await GetAllRolesQuery.GetAllRolesAsync(pageRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetAllRolesAsync_WhenRolesExist_ShouldReturnPagedRoles()
    {
        // Arrange
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = "admin" });
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = "editor" });
        await RolesRepository.SaveChangesAsync();

        var pageRequest = new PageRequest(1, 1);

        // Act
        var result = await GetAllRolesQuery.GetAllRolesAsync(pageRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(2, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetAllRolesAsync_WhenPaging_ShouldReturnCorrectPage()
    {
        // Arrange
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = "admin" });
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = "editor" });
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = "viewer" });
        await RolesRepository.SaveChangesAsync();

        var pageRequest = new PageRequest(2, 1);

        // Act
        var result = await GetAllRolesQuery.GetAllRolesAsync(pageRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("editor", result.Value.Items.Single().Name);
        Assert.Equal(3, result.Value.TotalCount);
    }
}