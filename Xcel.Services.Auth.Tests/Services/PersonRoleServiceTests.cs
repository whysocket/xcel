using Domain.Interfaces.Repositories.Shared;
using Xcel.Services.Auth.Implementations.Services;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Tests.Services;

public class PersonRoleServiceTests : AuthBaseTest
{
    private IPersonRoleService _personRoleService = null!;
    private Person _person = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _personRoleService = new PersonRoleService(PersonRoleRepository, RolesRepository);
        _person = await CreatePersonAsync();
    }

    [Fact]
    public async Task AddRoleToPersonAsync_WhenPersonIdOrRoleIdIsEmpty_ShouldReturnFailure()
    {
        // Arrange
        var roleId = Guid.Empty;

        // Act
        var result = await _personRoleService.AddRoleToPersonAsync(_person.Id, roleId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Errors.Single().Type);
        Assert.Equal("ApplicantId and RoleId must be valid GUIDs.", result.Errors.Single().Message);
    }

    [Fact]
    public async Task AddRoleToPersonAsync_WhenRoleDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var roleId = Guid.NewGuid();

        // Act
        var result = await _personRoleService.AddRoleToPersonAsync(_person.Id, roleId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Errors.Single().Type);
        Assert.Equal($"Role with ID '{roleId}' not found.", result.Errors.Single().Message);
    }

    [Fact]
    public async Task AddRoleToPersonAsync_WhenRoleAlreadyAssigned_ShouldReturnFailure()
    {
        // Arrange
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "Admin" };
        await RolesRepository.AddAsync(role);

        await PersonRoleRepository.AddAsync(new PersonRoleEntity { PersonId = _person.Id, RoleId = role.Id });
        await PersonRoleRepository.SaveChangesAsync();

        // Act
        var result = await _personRoleService.AddRoleToPersonAsync(_person.Id, role.Id);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Errors.Single().Type);
        Assert.Equal("This role is already assigned to the person.", result.Errors.Single().Message);
    }

    [Fact]
    public async Task AddRoleToPersonAsync_WhenValidPersonAndRole_ShouldAddRole()
    {
        // Arrange
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "Admin" };

        await RolesRepository.AddAsync(role);
        await RolesRepository.SaveChangesAsync();

        // Act
        var result = await _personRoleService.AddRoleToPersonAsync(_person.Id, role.Id);

        // Assert
        Assert.True(result.IsSuccess);

        var personRole = await PersonRoleRepository.GetPersonRoleAsync(_person.Id, role.Id);
        Assert.NotNull(personRole);
        Assert.Equal(_person.Id, personRole.PersonId);
        Assert.Equal(role.Id, personRole.RoleId);
    }

    [Fact]
    public async Task GetRolesForPersonAsync_WhenPersonIdIsEmpty_ShouldReturnFailure()
    {
        // Arrange
        var personId = Guid.Empty;

        // Act
        var result = await _personRoleService.GetRolesByPersonIdAsync(personId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Errors.Single().Type);
        Assert.Equal("ApplicantId must be a valid GUID.", result.Errors.Single().Message);
    }

    [Fact]
    public async Task GetRolesForPersonAsync_WhenPersonHasRoles_ShouldReturnRoles()
    {
        // Arrange
        var role1 = new RoleEntity { Id = Guid.NewGuid(), Name = "Admin" };
        var role2 = new RoleEntity { Id = Guid.NewGuid(), Name = "User" };

        await RolesRepository.AddRangeAsync([role1, role2]);
        await PersonRoleRepository.AddRangeAsync([
            new PersonRoleEntity { PersonId = _person.Id, RoleId = role1.Id },
            new PersonRoleEntity { PersonId = _person.Id, RoleId = role2.Id }
        ]);

        await PersonRoleRepository.SaveChangesAsync();

        // Act
        var result = await _personRoleService.GetRolesByPersonIdAsync(_person.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);

        var retrievedRole1 = Assert.Single(result.Value, personRoleEntity => personRoleEntity.Role.Id == role1.Id);
        Assert.Equal(role1.Id, retrievedRole1.Role.Id);
        Assert.Equal(role1.Name, retrievedRole1.Role.Name);

        var retrievedRole2 = Assert.Single(result.Value, personRoleEntity => personRoleEntity.Role.Id == role2.Id);
        Assert.Equal(role2.Id, retrievedRole2.Role.Id);
        Assert.Equal(role2.Name, retrievedRole2.Role.Name);
        
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task GetRolesForPersonAsync_WhenPersonHasNoRoles_ShouldReturnEmptyList()
    {
        // Arrange
        var personId = Guid.NewGuid();

        // Act
        var result = await _personRoleService.GetRolesByPersonIdAsync(personId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetPersonsByRoleId_WhenRoleExistsWithPersons_ShouldReturnPaginatedPersons()
    {
        // Arrange
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "Reviewer" };
        await RolesRepository.AddAsync(role);

        var person1 = await CreatePersonAsync();
        var person2 = await CreatePersonAsync();

        await PersonRoleRepository.AddRangeAsync([
            new PersonRoleEntity { PersonId = person1.Id, RoleId = role.Id },
            new PersonRoleEntity { PersonId = person2.Id, RoleId = role.Id }
        ]);

        await PersonRoleRepository.SaveChangesAsync();

        // Act
        var result = await _personRoleService.GetAllPersonsRolesByRoleIdAsync(
            role.Id,
            new PageRequest(1, 100));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Contains(result.Value.Items, p => p.PersonId == person1.Id);
        Assert.Contains(result.Value.Items, p => p.PersonId == person2.Id);
    }
    
    [Fact]
    public async Task RemoveRoleFromPersonAsync_WhenPersonIdOrRoleIdIsEmpty_ShouldReturnFailure()
    {
        // Arrange
        var personId = Guid.Empty;
        var roleId = Guid.NewGuid();

        // Act
        var result = await _personRoleService.RemoveRoleFromPersonAsync(personId, roleId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Errors.Single().Type);
        Assert.Equal("ApplicantId and RoleId must be valid GUIDs.", result.Errors.Single().Message);
    }

    [Fact]
    public async Task RemoveRoleFromPersonAsync_WhenRoleAssignmentDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var roleId = Guid.NewGuid();

        // Act
        var result = await _personRoleService.RemoveRoleFromPersonAsync(_person.Id, roleId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Errors.Single().Type);
        Assert.Equal("Role assignment not found for the person.", result.Errors.Single().Message);
    }

    [Fact]
    public async Task RemoveRoleFromPersonAsync_WhenRoleAssignmentExists_ShouldRemoveRole()
    {
        // Arrange
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "Admin" };
        await RolesRepository.AddAsync(role);

        await PersonRoleRepository.AddAsync(new PersonRoleEntity { PersonId = _person.Id, RoleId = role.Id });
        await PersonRoleRepository.SaveChangesAsync();

        // Act
        var result = await _personRoleService.RemoveRoleFromPersonAsync(_person.Id, role.Id);

        // Assert
        Assert.True(result.IsSuccess);

        var personRole = await PersonRoleRepository.GetPersonRoleAsync(_person.Id, role.Id);
        Assert.Null(personRole);
    }
}