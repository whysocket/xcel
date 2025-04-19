using Domain.Interfaces.Repositories.Shared;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Tests.Features.PersonRoles.Queries;

public class GetPersonRolesByRoleIdQueryTests : AuthBaseTest
{
    [Fact]
    public async Task GetPersonRolesByRoleIdAsync_WhenRoleHasNoAssignments_ShouldReturnEmptyPageResult()
    {
        // Arrange
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "TestRole" };
        await RolesRepository.AddAsync(role);
        await RolesRepository.SaveChangesAsync();

        var pageRequest = new PageRequest(1, 10);

        // Act
        var result = await GetPersonRolesByRoleIdQuery.ExecuteAsync(role.Id, pageRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetPersonRolesByRoleIdAsync_WhenRoleHasAssignments_ShouldReturnPagedAssignments()
    {
        // Arrange
        var person1 = await CreatePersonAsync();
        var person2 = await CreatePersonAsync();
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "TestRole" };
        await RolesRepository.AddAsync(role);
        await RolesRepository.SaveChangesAsync();

        await PersonRoleRepository.AddAsync(new PersonRoleEntity { PersonId = person1.Id, RoleId = role.Id });
        await PersonRoleRepository.AddAsync(new PersonRoleEntity { PersonId = person2.Id, RoleId = role.Id });
        await PersonRoleRepository.SaveChangesAsync();

        var pageRequest = new PageRequest(1, 1);

        // Act
        var result = await GetPersonRolesByRoleIdQuery.ExecuteAsync(role.Id, pageRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(2, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetPersonRolesByRoleIdAsync_WhenPaging_ShouldReturnCorrectPage()
    {
        // Arrange
        var person1 = await CreatePersonAsync();
        var person2 = await CreatePersonAsync();
        var person3 = await CreatePersonAsync();
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "TestRole" };
        await RolesRepository.AddAsync(role);
        await RolesRepository.SaveChangesAsync();

        await PersonRoleRepository.AddAsync(new PersonRoleEntity { PersonId = person1.Id, RoleId = role.Id });
        await PersonRoleRepository.AddAsync(new PersonRoleEntity { PersonId = person2.Id, RoleId = role.Id });
        await PersonRoleRepository.AddAsync(new PersonRoleEntity { PersonId = person3.Id, RoleId = role.Id });
        await PersonRoleRepository.SaveChangesAsync();

        var pageRequest = new PageRequest(2, 1);

        // Act
        var result = await GetPersonRolesByRoleIdQuery.ExecuteAsync(role.Id, pageRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(3, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetPersonRolesByRoleIdAsync_WhenInvalidRoleId_ShouldReturnFailureValidation()
    {
        // Arrange
        var invalidRoleId = Guid.Empty;
        var pageRequest = new PageRequest(1, 10);

        // Act
        var result = await GetPersonRolesByRoleIdQuery.ExecuteAsync(invalidRoleId, pageRequest);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(CommonErrors.InvalidGuid("roleId"), resultError);
    }
}