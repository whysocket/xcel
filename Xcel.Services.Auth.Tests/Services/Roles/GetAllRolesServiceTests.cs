using Domain.Interfaces.Repositories.Shared;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Tests.Services.Roles;

public class GetAllRolesServiceTests : AuthBaseTest
{
    [Fact]
    public async Task GetAllRolesAsync_WhenNoRolesExist_ShouldReturnEmptyPageResult()
    {
        // Arrange
        var pageRequest = new PageRequest(1, 10);

        // Act
        var result = await GetAllRolesService.GetAllRolesAsync(pageRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetAllRolesAsync_WhenRolesExist_ShouldReturnPagedRoles()
    {
        // Arrange
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = "Admin" });
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = "Editor" });
        await RolesRepository.SaveChangesAsync();

        var pageRequest = new PageRequest(1, 1);

        // Act
        var result = await GetAllRolesService.GetAllRolesAsync(pageRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(2, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetAllRolesAsync_WhenPaging_ShouldReturnCorrectPage()
    {
        // Arrange
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = "Admin" });
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = "Editor" });
        await RolesRepository.AddAsync(new RoleEntity { Id = Guid.NewGuid(), Name = "Viewer" });
        await RolesRepository.SaveChangesAsync();

        var pageRequest = new PageRequest(2, 1); // Get the second page with one item per page

        // Act
        var result = await GetAllRolesService.GetAllRolesAsync(pageRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Editor", result.Value.Items.Single().Name); // Assuming order is consistent
        Assert.Equal(3, result.Value.TotalCount);
    }
}