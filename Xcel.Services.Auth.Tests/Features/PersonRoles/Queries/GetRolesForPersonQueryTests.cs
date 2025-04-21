using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Tests.Features.PersonRoles.Queries;

public class GetRolesForPersonQueryTests : AuthBaseTest
{
    [Fact]
    public async Task GetRolesForPersonAsync_WhenPersonHasNoRoles_ShouldReturnEmptyList()
    {
        // Arrange
        var person = await CreateUserAsync();

        // Act
        var result = await GetRolesForPersonQuery.ExecuteAsync(person.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetRolesForPersonAsync_WhenPersonHasRoles_ShouldReturnListOfRoles()
    {
        // Arrange
        var person = await CreateUserAsync();
        var role1 = new RoleEntity { Id = Guid.NewGuid(), Name = "Role1" };
        var role2 = new RoleEntity { Id = Guid.NewGuid(), Name = "Role2" };
        await RolesRepository.AddAsync(role1);
        await RolesRepository.AddAsync(role2);
        await RolesRepository.SaveChangesAsync();

        await PersonRoleRepository.AddAsync(new PersonRoleEntity { PersonId = person.Id, RoleId = role1.Id });
        await PersonRoleRepository.AddAsync(new PersonRoleEntity { PersonId = person.Id, RoleId = role2.Id });
        await PersonRoleRepository.SaveChangesAsync();

        // Act
        var result = await GetRolesForPersonQuery.ExecuteAsync(person.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, r => r.RoleId == role1.Id);
        Assert.Contains(result.Value, r => r.RoleId == role2.Id);
    }

    [Fact]
    public async Task GetRolesForPersonAsync_WhenInvalidPersonId_ShouldReturnFailureValidation()
    {
        // Arrange
        var invalidPersonId = Guid.Empty;

        // Act
        var result = await GetRolesForPersonQuery.ExecuteAsync(invalidPersonId);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(CommonErrors.InvalidGuid("personId"), resultError);
    }
}